# Firebase.Realtime-Database

A [Firebase](https://firebase.google.com/) Realtime Database package for the [Unity](https://unity.com/) Game Engine.

[![openupm](https://img.shields.io/npm/v/com.firebase.realtime-database?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.firebase.realtime-database/)

## Installing

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Add the OpenUPM package registry:
  - `Name: OpenUPM`
  - `URL: https://package.openupm.com`
  - `Scope(s):`
    - `com.firebase`

![scoped-registries](Firebase.Realtime-Database/packages/com.firebase.realtime-database/Documentation~/images/package-manager-scopes.png)

- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Firebase.Realtime-Database` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/StephenHodgson/com.firebase.realtime-database.git#upm`

## Getting Started

```csharp
// Create a firebase authentication client
var authClient = new FirebaseAuthenticationClient();

// Create a firebase storage client
var dbClient = new FirebaseRealtimeDatabaseClient(authClient);

// Sign the user in
await authClient.SignInWithEmailAndPasswordAsync(email, password);

// Sets json data at the specified endpoint.
await dbClient.SetDataSnapshotAsync("test", "{\"value\":42}");

// Gets json data at the specified endpoint.
var snapshotValue = await dbClient.GetDataSnapshotAsync("test");
// snapshotValue == {"value":42}

// Deletes data at the specified endpoint.
await dbClient.DeleteDataSnapshotAsync("test");
```

## Additional Packages

- [Firebase.Authentication](https://github.com/StephenHodgson/com.firebase.authentication)
- [Firebase.Storage](https://github.com/StephenHodgson/com.firebase.storage)
