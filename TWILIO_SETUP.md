# Password reset by SMS (Twilio Verify): setup

Forgot password offers two options:

1. **Text me a code** (default): Twilio Verify texts a 6-digit code to the
   mobile number on the account. The code expires in 10 minutes.
2. **Answer my security questions**: for users without their phone.

## Works with no setup (development)

If the `Twilio` settings are empty, no SMS is sent. The code is printed in the
console window, for example:

    Twilio is not configured, so no SMS was sent. Verification code for +27823456789: 786593

It is also saved in `CareerSyncAI/App_Data/DevSms/`. Type it on the "Enter your code" page.

## Connecting your Twilio account

You need three values. Two come from the Twilio Console home page:

| Setting | Where to find it | Looks like |
|---|---|---|
| `AccountSid` | Console home → Account Info | `AC` + 32 characters |
| `AuthToken` | Console home → Account Info (click **Show**) | 32 characters, **secret** |
| `VerifyServiceSid` | Create it in step 1 below | `VA` + 32 characters |

### 1. Create a Verify service (one-off)

Twilio Console → **Verify** → **Services** → **Create new**:
- Friendly name: `CareerSync AI`. This name appears in the SMS:
  "Your CareerSync AI verification code is: …"
- Verification channels: tick **SMS**.
- Save, then copy the **Service SID** (starts with `VA`).

### 2. Trial account only: verify your test phones

A trial account can only text numbers you have verified yourself:
Twilio Console → **Phone Numbers** → **Manage** → **Verified Caller IDs** → **Add**.
Trial accounts allow up to 5 verified numbers and 40 free verifications over 30 days.

Make sure the graduate account you test with has that same number as its
mobile number (**Profile** page in the app).

### 3. Put the values in User Secrets (never in appsettings.json)

In Visual Studio: right-click the **CareerSyncAI** project → **Manage User Secrets**,
then paste and fill in:

```json
{
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "your-auth-token",
    "VerifyServiceSid": "VAxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

Or from a terminal in the `CareerSyncAI` project folder:

```
dotnet user-secrets set "Twilio:AccountSid" "ACxxxxxxxx..."
dotnet user-secrets set "Twilio:AuthToken" "your-auth-token"
dotnet user-secrets set "Twilio:VerifyServiceSid" "VAxxxxxxxx..."
```

User Secrets are stored outside the project folder, so they never end up in Git
or in a zip you hand in.

### 4. Restart and test

Restart the app. The console no longer says "Twilio is not configured".
Choose **Forgot password? → Text me a code**, and the SMS should arrive within seconds.

## Production (IIS / Azure)

Use environment variables instead of User Secrets, with a double underscore for each level:

```
Twilio__AccountSid=AC...
Twilio__AuthToken=...
Twilio__VerifyServiceSid=VA...
```

## Phone numbers

Users type local numbers (`082 345 6789`). The app converts them to the format
Twilio needs (`+27823456789`). Numbers without a country code are treated as
South African; others need the code, e.g. `+44 7700 900123`. Register, Profile
and Admin → Edit graduate all reject numbers that can't be converted.

## Troubleshooting

When a text can't be sent, the user sees "We couldn't send a text right now…",
and the console shows the exact Twilio error with a hint:

| Console shows | Meaning / fix |
|---|---|
| `Twilio error 20003` | Wrong AccountSid or AuthToken |
| `Twilio error 20404` | Wrong VerifyServiceSid (must start with `VA`) |
| `Twilio error 21608` / `21610` | Trial account: the number isn't a Verified Caller ID (step 2) |
| `Twilio error 60200` / `21211` | The user's number isn't a valid mobile number |
| `Twilio error 60203` | Too many codes sent to this number; Twilio allows more later |

Other limits built into the app:
- At most one text per account per minute.
- After 5 wrong codes, the user must request a new code.
