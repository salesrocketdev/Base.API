using Base.Core.Email.Models;

namespace Base.Core.Email;

public interface ISendMailService
{
    #region Enqueue Methods (Background Jobs)

    void EnqueueWelcomeEmail(string toEmail, WelcomeModel model);
    void EnqueueVerificationCodeEmail(string toEmail, VerificationCodeModel model);

    #endregion

    #region Email Sending Methods

    Task<bool> SendWelcomeEmail(string email, WelcomeModel model);
    Task<bool> SendVerificationCodeEmail(string email, VerificationCodeModel model);

    #endregion
}
