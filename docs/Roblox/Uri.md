# Roblox URI

Roblox supports the `roblox://` URI scheme for launching the Roblox client
with parameters describing the experience or server to join.

## Public Server

```text
roblox://placeId=1234567890
````

A URI containing only `placeId` launches the Roblox client and joins a
public server for the specified place.

## Parameters

| Parameter                  | Description                                 |
| -------------------------- | ------------------------------------------- |
| `placeId`                  | Roblox place ID.                            |
| `gameInstanceId`           | Game/server instance identifier.            |
| `accessCode`               | Server access code.                         |
| `linkCode`                 | Roblox link code.                           |
| `launchData`               | Launch data passed to the client.           |
| `joinAttemptId`            | Identifier for the join attempt.            |
| `joinAttemptOrigin`        | Origin associated with the join attempt.    |
| `reservedServerAccessCode` | Access code for a reserved server.          |
| `callId`                   | Identifier associated with the launch call. |
| `browserTrackerId`         | Browser tracker identifier.                 |
