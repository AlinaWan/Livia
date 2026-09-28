# Auto Order

## User Guide

### Order Automation

The core Auto Order application will automatically order ingredients for you.

1. Set the `Item Count` to the **total number of Drinks + Toppings + Pastries** in My Cafe. Use the absolute total even if you haven't unlocked every ingredient.
2. Open the **Order tab** in the main menu
3. Press '`\`' to enable **UI navigation**. There should be a blue rectangle somewhere on your screen. You **do not** need to move it to a specific location before starting the macro.
4. Press '`F6`' to Start / Stop the macro.

### Auto Rejoin

Auto Order can automatically rejoin your Roblox account if the Roblox client is disconnected (loss of internet, client timeout, etc.).

1. Toggle `Rejoin on Disconnect` to on (the `⏽` symbol should be highlighted).
2. Set your `Rejoin URL`. You can also press the "`Get`" button to automatically retrieve one.
   * If the Auto Order application is running as **Administrator**, it will automatically fetch the **first available private server URL** under your primary browser's logged-in Roblox account[^1].
   * If Auto Order is not running as Administrator or it fails to retrieve your private server URL automatically, it will use a Roblox URI to a **random public server** (although you can always **manually paste in your private server link**).

The application will try up to `3` attempts to rejoin the game with a timeout of `60` seconds. If it fails after `3` attempts, Auto Order will automatically initiate a scheduled shutdown to save electricity
while you're away, so your computer doesn't run idle. You can abort this shutdown by pressing <kbd>Esc</kbd> at any time during the `60` second window after the scheduled shutdown notification.

### Alt Account Joining

Auto Order can automatically join all your alts into the same server so you can receive the maximum friend boost. **Alt account joining requires the application to be run as Administrator**[^4].

1. In the `Tokens` tab, add the `.ROBLOSECURITY` tokens[^2] of each of your alt accounts.
2. Press the "`Join Accounts`" button to automatically authenticate and launch a client per alt account.

The application will use your **primary browser's logged-in Roblox account as the host**, and will automatically fetch the **first available private server access code** under it. **Your alt accounts must have direct access to this server**[^3] (not by the invite link).

My Cafe already automatically handles rejoining clients to prevent being kicked for idle.

Note that the accounts you add to this list **will not save** when you close the Auto Order application. You should store your alt accounts' tokens somewhere safe to be pasted back in when you restart the application.

## FAQ

**Q: Do I need to have every ingredient unlocked?**  
**A:** No, just be sure to set the `Item Count` to the **total number of all ingredients** in the game, even if you haven't unlocked them yet.

**Q: Why does Auto Order need to be run as Administrator/need my tokens?**  
**A:** You **do not need to run the application as Administrator or paste in any tokens** to use the core functionality of automating ordering. Auto rejoin does not require Administrator unless you want it to automatically fetch your private server link for you. The only feature which strictly requires Administrator/tokens is Alt Account Joining, and your **data is never transmitted**, except directly to Roblox.

**Q: Does Auto Order steal my tokens?**  
**A:** No, **everything is kept on your local device**, and the source code is publicly available for everyone to see. Your `.ROBLOSECURITY` tokens are only used to fetch your private server link **directly from Roblox** or fetch the authentication tickets to join your alt accounts.

[^1]: **IMPORTANT: The private server link must already have been generated.** You can generate your private server link by pressing the pencil icon beside your private server, then pressing "`Generate`" beside `Private Server Link`. Auto Order will **not** automatically generate a private server link for you.
[^2]: You can generate persistent `.ROBLOSECURITY` tokens for your alt accounts by opening a new incognito browser, logging into the account, pressing <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd> to open Developer Tools, navigating to `Application` -> `Storage` -> `Cookies` -> `https://www.roblox.com`, copying the value of the cookie named `.ROBLOSECURITY`, then closing the incognito browser. **Do not log out of the account, or the token will be invalidated.**
[^3]: You can give your alt accounts direct access to your private server by pressing the pencil icon beside your private server, then pressing "`Add People`" beside `Server Members`. If you're friends with your alt accounts, you can also just toggle `Friends Allowed` to on.
[^4]: You can run an application as Administrator by right-clicking the `.exe` file, then clicking "`Run as administrator`".