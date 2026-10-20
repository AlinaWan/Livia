using System.Globalization;

namespace Livia.Services.Configuration;

/// <summary>Evaluates a restricted arithmetic expression language without executing arbitrary code.</summary>
public sealed class ConfigurationExpressionEvaluator
{
    private readonly IReadOnlyDictionary<string, double> _variables;

    /// <summary>Creates an evaluator with caller-defined numeric variables.</summary>
    public ConfigurationExpressionEvaluator(IReadOnlyDictionary<string, double>? variables = null) =>
        _variables = variables ?? new Dictionary<string, double>();

    /// <summary>Evaluates an expression and returns its numeric result.</summary>
    public double Evaluate(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        Parser parser = new(expression, _variables);
        double result = parser.ParseExpression();
        parser.EnsureEnd();
        if (!double.IsFinite(result)) throw new FormatException("Expression result is not finite.");
        return result;
    }

    private sealed class Parser(string text, IReadOnlyDictionary<string, double> variables)
    {
        private int _position;
        private Token _current;
        private enum TokenKind { End, Number, Identifier, Plus, Minus, Star, Slash, FloorSlash, Percent, Power, Ampersand, Pipe, Caret, LeftParen, RightParen, Comma }
        private readonly record struct Token(TokenKind Kind, string Text, double Number = 0);

        public double ParseExpression() { Advance(); return ParseBitwiseOr(); }
        public void EnsureEnd() { if (_current.Kind != TokenKind.End) throw Error($"Unexpected token '{_current.Text}'."); }

        private double ParseBitwiseOr()
        {
            double value = ParseBitwiseXor();
            while (_current.Kind == TokenKind.Pipe) { Advance(); value = ToInteger(value) | ToInteger(ParseBitwiseXor()); }
            return value;
        }
        private double ParseBitwiseXor()
        {
            double value = ParseBitwiseAnd();
            while (_current.Kind == TokenKind.Caret) { Advance(); value = ToInteger(value) ^ ToInteger(ParseBitwiseAnd()); }
            return value;
        }
        private double ParseBitwiseAnd()
        {
            double value = ParseAdditive();
            while (_current.Kind == TokenKind.Ampersand) { Advance(); value = ToInteger(value) & ToInteger(ParseAdditive()); }
            return value;
        }
        private double ParseAdditive()
        {
            double value = ParseMultiplicative();
            while (_current.Kind is TokenKind.Plus or TokenKind.Minus)
            {
                TokenKind op = _current.Kind; Advance(); double rhs = ParseMultiplicative();
                value = op == TokenKind.Plus ? value + rhs : value - rhs;
            }
            return value;
        }
        private double ParseMultiplicative()
        {
            double value = ParseUnary();
            while (_current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.FloorSlash or TokenKind.Percent)
            {
                TokenKind op = _current.Kind; Advance(); double rhs = ParseUnary();
                value = op switch
                {
                    TokenKind.Star => value * rhs,
                    TokenKind.Slash => value / rhs,
                    TokenKind.FloorSlash => Math.Floor(value / rhs),
                    TokenKind.Percent => value % rhs,
                    _ => value
                };
            }
            return value;
        }
        private double ParseUnary()
        {
            if (_current.Kind == TokenKind.Plus) { Advance(); return ParseUnary(); }
            if (_current.Kind == TokenKind.Minus) { Advance(); return -ParseUnary(); }
            return ParsePower();
        }
        private double ParsePower()
        {
            double value = ParsePrimary();
            if (_current.Kind == TokenKind.Power) { Advance(); value = Math.Pow(value, ParseUnary()); }
            return value;
        }
        private double ParsePrimary()
        {
            if (_current.Kind == TokenKind.Number) { double number = _current.Number; Advance(); return number; }
            if (_current.Kind == TokenKind.LeftParen)
            {
                Advance(); double value = ParseBitwiseOr(); Expect(TokenKind.RightParen); Advance(); return value;
            }
            if (_current.Kind == TokenKind.Identifier)
            {
                string name = _current.Text; Advance();
                if (_current.Kind != TokenKind.LeftParen)
                {
                    if (variables.TryGetValue(name, out double value)) return value;
                    throw Error($"Unknown variable '{name}'.");
                }
                Advance(); List<double> args = [];
                if (_current.Kind != TokenKind.RightParen)
                {
                    args.Add(ParseBitwiseOr());
                    while (_current.Kind == TokenKind.Comma) { Advance(); args.Add(ParseBitwiseOr()); }
                }
                Expect(TokenKind.RightParen); Advance();
                return name switch
                {
                    "abs" when args.Count == 1 => Math.Abs(args[0]),
                    "int" when args.Count == 1 => Math.Truncate(args[0]),
                    "round" when args.Count == 1 => Math.Round(args[0], MidpointRounding.ToEven),
                    "round" when args.Count == 2 && args[1] == Math.Truncate(args[1]) && args[1] is >= 0 and <= 15 => Math.Round(args[0], (int)args[1], MidpointRounding.ToEven),
                    "min" when args.Count >= 1 => args.Min(),
                    "max" when args.Count >= 1 => args.Max(),
                    _ => throw Error($"Function '{name}' is not supported for the supplied arguments.")
                };
            }
            throw Error("Expected a number, variable, or parenthesized expression.");
        }
        private static long ToInteger(double value)
        {
            if (!double.IsFinite(value) || value != Math.Truncate(value) || value < long.MinValue || value > long.MaxValue)
                throw new FormatException("Bitwise operators require integral 64-bit operands.");
            return (long)value;
        }
        private void Expect(TokenKind kind) { if (_current.Kind != kind) throw Error($"Expected {kind}."); }
        private FormatException Error(string message) => new($"{message} (position {_position}).");
        private void Advance()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position])) _position++;
            if (_position >= text.Length) { _current = new(TokenKind.End, ""); return; }
            int start = _position; char c = text[_position++];
            if (char.IsDigit(c) || c == '.')
            {
                if (c == '0' && _position < text.Length && (text[_position] == 'x' || text[_position] == 'X'))
                {
                    _position++; int hexStart = _position;
                    while (_position < text.Length && Uri.IsHexDigit(text[_position])) _position++;
                    if (hexStart == _position || !long.TryParse(text[hexStart.._position], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hex)) throw Error("Invalid hexadecimal integer.");
                    _current = new(TokenKind.Number, text[start.._position], hex); return;
                }
                while (_position < text.Length && (char.IsDigit(text[_position]) || text[_position] == '.')) _position++;
                if (_position < text.Length && (text[_position] == 'e' || text[_position] == 'E'))
                {
                    _position++; if (_position < text.Length && (text[_position] == '+' || text[_position] == '-')) _position++;
                    while (_position < text.Length && char.IsDigit(text[_position])) _position++;
                }
                string numberText = text[start.._position];
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) throw Error("Invalid numeric literal.");
                _current = new(TokenKind.Number, numberText, number); return;
            }
            if (char.IsLetter(c) || c == '_')
            {
                while (_position < text.Length && (char.IsLetterOrDigit(text[_position]) || text[_position] == '_')) _position++;
                _current = new(TokenKind.Identifier, text[start.._position]); return;
            }
            TokenKind kind = c switch
            {
                '+' => TokenKind.Plus, '-' => TokenKind.Minus, '*' when _position < text.Length && text[_position] == '*' => ConsumeDouble(TokenKind.Power),
                '*' => TokenKind.Star, '/' when _position < text.Length && text[_position] == '/' => ConsumeDouble(TokenKind.FloorSlash),
                '/' => TokenKind.Slash, '%' => TokenKind.Percent, '&' => TokenKind.Ampersand, '|' => TokenKind.Pipe, '^' => TokenKind.Caret,
                '(' => TokenKind.LeftParen, ')' => TokenKind.RightParen, ',' => TokenKind.Comma,
                _ => throw Error($"Unsupported character '{c}'.")
            };
            _current = new(kind, text[start.._position]);
        }
        private TokenKind ConsumeDouble(TokenKind kind) { _position++; return kind; }
    }
}
