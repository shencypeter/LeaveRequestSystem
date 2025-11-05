using BioMedDocManager.Models;
using Microsoft.AspNetCore.Identity;

public static class PasswordUtil
{
    
    
    /// <summary>
    /// 檢查密碼是否被hash過
    /// </summary>
    /// <param name="password">密碼(可能有hash，也可能沒有)</param>
    /// <returns></returns>
    public static bool AlreadyHashed(string password)
    {
        return password.StartsWith("AQAAAA", StringComparison.Ordinal) && password.Length >= 80;
    }

    /// <summary>
    /// hash密碼
    /// </summary>
    /// <param name="plainTextPassword">原始密碼</param>
    /// <returns></returns>
    public static string Hash(string plainTextPassword)
    {
        var hasher = new PasswordHasher<string>();
        return hasher.HashPassword(null, plainTextPassword);
    }

    /// <summary>
    /// 驗證密碼
    /// </summary>
    /// <param name="hashedPassword">hash過的密碼</param>
    /// <param name="inputPassword">輸入的密碼</param>
    /// <returns></returns>
    public static bool Verify(string hashedPassword, string inputPassword)
    {
        var hasher = new PasswordHasher<string>();
        var result = hasher.VerifyHashedPassword(null, hashedPassword, inputPassword);
        return result == PasswordVerificationResult.Success;
    }
}
