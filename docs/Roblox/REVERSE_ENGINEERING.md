# Reverse Engineering

## Roblox Web API Endpoints

Updated: 2026-09-24

Roblox's website uses HTTP endpoints to retrieve much of the data displayed in its web interface. These endpoints are not always obvious from Roblox's Cloud API documentation, and the JavaScript responsible for making the requests can be heavily obfuscated.

This guide documents a practical method for finding these endpoints by intercepting the requests made by the Roblox website itself.

The method described here was used to identify the private-server endpoints used to retrieve private server information and join links.

---

### The basic idea

Instead of trying to determine the endpoint from Roblox's JavaScript, intercept the browser's network functions and inspect responses as the Roblox website receives them.

The workflow is:

```text
Find a value you already know
        ↓
Install an Override on the Roblox page
        ↓
Interceptor hooks XHR + Fetch
        ↓
Reload the Roblox page
        ↓
Perform the action that loads the data
(which may have been by simply reloading the page)
        ↓
Interceptor finds the value in a response
        ↓
Debugger pauses execution
        ↓
Inspect URL + HTTP method + response
        ↓
Reproduce the request with HttpClient
```

The important part is that the interceptor is installed **before the page makes the requests you are interested in**.

---

### 1. Find a value to search for

First, determine something that should appear in the response you are trying to find.

A specific ID is ideal.

For example:

```text
1234567890
```

If you are investigating private servers and already know a server ID, searching for that exact value is much more useful than searching for something generic.

Other useful values can be fields or strings that you know should appear in the response:

```text
vipServerId
joinCode
accessCode
```

For example, while investigating private-server functionality, searching for:

```text
vipServerId
```

can reveal the response containing the private server list.

Searching for:

```text
joinCode
```

can help locate the response containing the server's join information.

---

### 2. Open the Roblox page in Chromium

Open the relevant Roblox page in Chrome, Edge, Vivaldi, or another Chromium-based browser.

Open Developer Tools with:

```text
F12
```

The important part of this process is **Sources → Overrides**.

Do not simply paste the interceptor into the Console.

If you put the script directly into the Console, it will disappear when the page is reloaded. Reloading is important because the requests that populate the page may happen during page initialization, before you have an opportunity to install the interceptor again.

---

### 3. Create a Local Override

In Developer Tools:

```text
Sources
    ↓
Overrides
```

Enable Overrides and select a local directory when prompted.

You need to override an actual Roblox webpage rather than creating an arbitrary JavaScript file.

The easiest way to find the correct file is to open the **Network** tab and reload the Roblox page.

Look for the first network entry that represents the actual webpage.

Opening that request should show a **Preview** containing the Roblox page/document rather than an ordinary API response.

That is the page you want to override.

The important distinction is:

```text
Network
├── document/page        ← override this
├── JavaScript files
├── API requests
├── images
└── other resources
```

The interceptor needs to execute as part of the actual Roblox page. The interceptor function can be added immediately after the header.

---

### 4. Add the Network Interceptor to the Override

Once the Roblox page has been overridden, place the interceptor in that override.

The following script intercepts both `XMLHttpRequest` and `fetch`.

It searches all responses for a specific value.

```javascript
(function() {
    const targetId = "1234567890";
    console.log(`[*] Interceptor Active. Scanning network traffic for: ${targetId}`);

    // 1. Intercept XMLHttpRequest
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;

    XMLHttpRequest.prototype.open = function(method, url) {
        this._method = method;
        this._url = url;
        return originalOpen.apply(this, arguments);
    };

    XMLHttpRequest.prototype.send = function() {
        this.addEventListener('load', function() {
            try {
                const text = this.responseText;

                if (text && text.includes(targetId)) {
                    console.log(`[+] FOUND in XHR Request!`, {
                        url: this._url,
                        method: this._method,
                        response: JSON.parse(text)
                    });

                    debugger;
                }
            } catch (e) {}

        });

        return originalSend.apply(this, arguments);
    };

    // 2. Intercept Fetch API
    const originalFetch = window.fetch;

    window.fetch = async function(...args) {
        const response = await originalFetch.apply(this, args);

        try {
            const clone = response.clone();
            const text = await clone.text();

            if (text && text.includes(targetId)) {
                console.log(`[+] FOUND in Fetch Request!`, {
                    url: args[0],
                    response: JSON.parse(text)
                });

                debugger;
            }
        } catch (e) {}

        return response;
    };
})();
```

This does two things.

#### XMLHttpRequest

It replaces:

```javascript
XMLHttpRequest.prototype.open
```

and:

```javascript
XMLHttpRequest.prototype.send
```

so it can record the request's:

```text
HTTP method
URL
```

and inspect the response when it finishes.

#### Fetch

It replaces:

```javascript
window.fetch
```

and examines the returned response.

The response is cloned before reading it:

```javascript
const clone = response.clone();
```

so the interceptor can inspect the response without consuming the response that Roblox expects to receive.

---

### 5. Reload the Roblox Page

After installing the override, **reload the page**.

This is important.

The purpose of putting the interceptor in the page override is that it executes during page loading, before Roblox starts making the requests you are interested in.

If the interceptor is only pasted into the Console after the page has already loaded, requests made during initialization will already have happened.

The intended sequence is:

```text
Install Override
      ↓
Reload Roblox
      ↓
Interceptor executes
      ↓
Roblox initializes
      ↓
XHR / Fetch requests occur
      ↓
Responses are inspected
```

---

### 6. Perform the Action That Loads the Data

Now use the Roblox website normally.

For example, when investigating private-server information, navigate to the relevant private-server interface or perform the action that causes Roblox to retrieve the server data.

The interceptor examines the responses automatically.

When it finds the target value, you should see something similar to:

```text
[+] FOUND in XHR Request!
{
    url: "...",
    method: "GET",
    response: {...}
}
```

or:

```text
[+] FOUND in Fetch Request!
{
    url: "...",
    response: {...}
}
```

The:

```javascript
debugger;
```

statement will also pause JavaScript execution.

At that point, the interesting information is:

```text
URL
HTTP method
Response JSON
```

---

### 7. Searching for JSON Properties Instead

Sometimes there isn't a specific ID you already know.

In that case, search for a property that identifies the response you're looking for.

For example:

```javascript
if (
    text.includes('accessCode') ||
    text.includes('joinCode') ||
    text.includes('1a2b3c4d')
) {
    console.log("[+] Caught response:", this._url, JSON.parse(text));
    debugger;
}
```

The same condition can be used in the Fetch interceptor.

For example:

```javascript
if (
    text.includes('accessCode') ||
    text.includes('joinCode') ||
    text.includes('1a2b3c4d')
) {
    console.log("[+] Caught fetch response:", args[0], JSON.parse(text));
    debugger;
}
```

The important part is choosing a string that is relevant to the specific data you're looking for.

---

### 8. Example: Finding the Private Server List Endpoint

When investigating private servers, a useful search target is:

```text
vipServerId
```

The response containing that property can reveal the endpoint that provides the private server list.

Once the interceptor pauses, inspect the response.

For example, a response may contain data resembling:

```json
{
    "data": [
        {
            "name": "My Private Server",
            "vipServerId": 123456789
        }
    ]
}
```

The request URL then identifies the endpoint responsible for providing that information.

That endpoint can subsequently be reproduced directly with an HTTP client.

---

### 9. Reproduce the Request

Once the endpoint has been identified, inspect the request's details.

Pay particular attention to:

```text
Request URL
HTTP method
Cookies
Authorization
Query parameters
Request body
```

Then reproduce the request with `HttpClient`.

For example:

```csharp
using var request = new HttpRequestMessage(
    HttpMethod.Get,
    endpoint);

request.Headers.Add(
    "Cookie",
    $".ROBLOSECURITY={token}");
```

---

### 10. Create DTOs From the Response

Once the endpoint works outside the browser, model the response using DTOs.

For example:

```csharp
public class VipServerInstanceDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("vipServerId")]
    public long VipServerId { get; set; }
}
```

Roblox's web APIs and Open Cloud APIs are separate API surfaces. An endpoint observed from the Roblox website should therefore be treated as a website/legacy API dependency rather than automatically assuming it has the same stability guarantees as an Open Cloud API.
