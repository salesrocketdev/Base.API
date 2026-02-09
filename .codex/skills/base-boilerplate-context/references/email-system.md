# Email System

## Current provider
- ZeptoMail, implemented by `Base.Core/Email/SendMailService.cs`.

## Queue model
- Domain services call enqueue methods from `ISendMailService`.
- `SendMailService` uses `Hangfire.BackgroundJob.Enqueue(...)`.
- Email sending runs asynchronously and is treated as non-critical.

## Current triggers
- Signup flow enqueues welcome email (`AuthService.RegisterAsync`).
- Forgot/reset flow enqueues verification/token email (`AuthService.InitiatePasswordResetAsync`).

## Config keys
- App settings:
  - `ZeptoMailSettings:Url`
  - `ZeptoMailSettings:Token`
  - `ZeptoMailSettings:FromAddress`
  - `ZeptoMailSettings:FromName`
- Environment:
  - `ZEPTOMAILSETTINGS__TOKEN`
- Queue:
  - `Hangfire:Enabled`
  - `Hangfire:UseDashboard`

## Development mode behavior
- If token is not configured, service uses development fallback and logs payload instead of sending.
- Errors during enqueue/send are logged and do not fail the auth request.

## Boilerplate hardening checklist for derived projects
- Replace template keys (`mail_template_key`) with real keys.
- Replace placeholder app URL in template merge info.
- Consider moving template keys to configuration.
- Decide if email should remain best-effort or become critical for specific flows.
- Add integration tests/mocks for enqueue behavior if email is business-critical.
