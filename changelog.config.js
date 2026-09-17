'use strict';

const commitTypes = {
  feat: 'Features',
  fix: 'Bug Fixes',
  docs: 'Documentation',
  refactor: 'Code Refactoring',
  ci: 'Continuous Integration'
};

const isBreakingChange = commit =>
  commit.breaking ||
  commit.type === 'BREAKING CHANGE' ||
  commit.notes?.some(note => note.title === 'BREAKING CHANGE');

module.exports = {
  parserOpts: {
    headerPattern: /^(\w+)(?:\((.+)\))?(!)?:\s+(.+)$/,
    headerCorrespondence: ['type', 'scope', 'breaking', 'subject']
  },

  writerOpts: {
    groupBy: 'type',

    transform(commit) {
      const result = { ...commit };

      if (isBreakingChange(result)) {
        result.type = '⚠ BREAKING CHANGES';
      } else {
        result.type = commitTypes[result.type];

        if (!result.type) {
          return null;
        }
      }

      return result;
    },

    commitGroupsSort: (a, b) => {
      if (a.title === '⚠ BREAKING CHANGES') return -1;
      if (b.title === '⚠ BREAKING CHANGES') return 1;

      return a.title.localeCompare(b.title);
    },

    mainTemplate: `## ${process.env.RELEASE_VERSION} ({{date}})
{{#each commitGroups}}

### {{title}}

{{#each commits}}
* {{header}}
{{/each}}
{{/each}}
`
  }
};