using CareerSyncAI.Models;
using CareerSyncAI.Models.Enums;
using CareerSyncAI.Services;

namespace CareerSyncAI.Data;


public static class SeedIdentity
{
    public const string AdminFullName = "Admin";
    public const string AdminEmail = "admin@ufs.ac.za";
    public const string AdminPassword = "Admin123!";

    public const string GraduateFullName = "Sbahle Dlamini";
    public const string GraduateEmail = "2018287165@ufs4life.ac.za";
    public const string GraduatePassword = "Graduate123!";

    public static readonly string DemoQuestion1 = SecurityQuestions.All[1];
    public const string DemoAnswer1 = "Bloemfontein";
    public static readonly string DemoQuestion2 = SecurityQuestions.All[0]; 
    public const string DemoAnswer2 = "Rex";


    public static void Seed(IRepositoryWrapper repository)
    {
        if (!repository.AppUser.EmailExists(AdminEmail))
        {
            repository.AppUser.Add(WithDemoSecurityQuestions(new AppUser
            {
                FullName = AdminFullName,
                Email = AdminEmail,
                PasswordHash = PasswordHasher.Hash(AdminPassword),
                Role = UserRole.Admin,
                Department = "Careers & Employability Office",
                PhoneNumber = "051 401 9111",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
            }));
        }

        if (!repository.AppUser.EmailExists(GraduateEmail))
        {
            repository.AppUser.Add(WithDemoSecurityQuestions(new AppUser
            {
                FullName = GraduateFullName,
                Email = GraduateEmail,
                PasswordHash = PasswordHasher.Hash(GraduatePassword),
                Role = UserRole.Graduate,
                Qualification = "BSc Computer Science",
                PhoneNumber = "082 345 6789",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
            }));
        }

        if (!repository.AppUser.EmailExists("2020115678@ufs4life.ac.za"))
        {
            repository.AppUser.Add(WithDemoSecurityQuestions(new AppUser
            {
                FullName = "Collen Dlamini",
                Email = "2020115678@ufs4life.ac.za",
                PasswordHash = PasswordHasher.Hash("Collen123!"),
                Role = UserRole.Graduate,
                Qualification = "BSc Information Technology",
                PhoneNumber = "071 234 5678",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
            }));
        }

        if (!repository.AppUser.EmailExists("202876123@ufs4life.ac.za"))
        {
            repository.AppUser.Add(WithDemoSecurityQuestions(new AppUser
            {
                FullName = "Mthokozisi Sibiya",
                Email = "202876123@ufs4life.ac.za",
                PasswordHash = PasswordHasher.Hash("Mthoko123!"),
                Role = UserRole.Graduate,
                Qualification = "BSc Cybersecurity",
                PhoneNumber = "083 456 7890",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
            }));
        }

        if (!repository.AppUser.EmailExists("2019234110@ufs4life.ac.za"))
        {
            repository.AppUser.Add(WithDemoSecurityQuestions(new AppUser
            {
                FullName = "Nkosinathi Ngubane",
                Email = "2019234110@ufs4life.ac.za",
                PasswordHash = PasswordHasher.Hash("Nathi123!"),
                Role = UserRole.Graduate,
                Qualification = "BSc Data Science",
                PhoneNumber = "079 567 8901",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
            }));
        }

        foreach (var user in repository.AppUser.GetAll().Where(u => !u.HasSecurityQuestions).ToList())
        {
            if (!IsDemoAccount(user.Email)) continue;
            repository.AppUser.Update(WithDemoSecurityQuestions(user));
        }
    }

    private static AppUser WithDemoSecurityQuestions(AppUser user)
    {
        user.SecurityQuestion1 = DemoQuestion1;
        user.SecurityAnswer1Hash = SecurityAnswerHasher.Hash(DemoAnswer1);
        user.SecurityQuestion2 = DemoQuestion2;
        user.SecurityAnswer2Hash = SecurityAnswerHasher.Hash(DemoAnswer2);
        return user;
    }

    private static bool IsDemoAccount(string email) => new[]
    {
        AdminEmail, GraduateEmail,
        "2020115678@ufs4life.ac.za", "202876123@ufs4life.ac.za", "2019234110@ufs4life.ac.za",
    }.Contains(email, StringComparer.OrdinalIgnoreCase);
}
