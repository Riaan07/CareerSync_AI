using CareerSyncAI.Models;

namespace CareerSyncAI.Data;

public static class SeedSkillDictionary
{
    private static readonly string[] Skills =
    {
        // Programming languages
        "C#", "Java", "Python", "JavaScript", "TypeScript", "C++", "Go", "Rust",
        "Kotlin", "Swift", "PHP", "Ruby", "R", "MATLAB",

        // Web development
        "HTML", "CSS", "React", "Angular", "Vue.js", "Node.js", "ASP.NET", "ASP.NET Core",
        "Django", "Flask", "Spring Boot", "REST API", "GraphQL", "Bootstrap",
        "Tailwind CSS", "Next.js", "Express.js",

        // Databases
        "SQL", "SQL Server", "MySQL", "PostgreSQL", "MongoDB", "Oracle Database",
        "Redis", "Entity Framework", "Database Design", "NoSQL",

        // Cloud & DevOps — expanded coverage (cloud engineering specifically requested)
        "Azure", "AWS", "Google Cloud Platform", "Docker", "Kubernetes", "CI/CD",
        "Jenkins", "Terraform", "DevOps", "Cloud Computing", "Cloud Engineering",
        "Cloud Architecture", "Serverless Computing", "Ansible", "GitHub Actions",
        "Infrastructure as Code", "Microsoft Azure Administrator", "Site Reliability Engineering",

        // Data science & AI
        "Machine Learning", "Deep Learning", "Data Science", "Data Analysis",
        "TensorFlow", "PyTorch", "Pandas", "NumPy", "Natural Language Processing",
        "Computer Vision", "Big Data", "PowerBI", "Tableau", "Excel",

        // Cybersecurity — expanded coverage (specifically requested)
        "Cybersecurity", "Network Security", "Penetration Testing", "Ethical Hacking",
        "Information Security", "Cryptography", "Vulnerability Assessment",
        "Incident Response", "Risk Management", "Firewall Configuration",
        "Security Operations", "SIEM", "Identity and Access Management",
        "Cloud Security", "Application Security", "Digital Forensics",
        "Security Auditing", "Threat Intelligence", "Zero Trust Security",

        // Networking & systems
        "networking", "Windows Server", "Linux", "Network Administration", "TCP/IP",
        "Cisco", "Active Directory", "Virtualization", "VMware", "IT Support",
        "System Administration",

        // Software engineering practice
        "Git", "GitHub", "Unit Testing", "xUnit", "Agile", "Scrum",
        "Object-Oriented Programming", "Software Design Patterns", "UML",
        "Microservices", "Software Architecture",

        // Mobile & game development
        "Android Development", "iOS Development", "React Native", "Flutter", "Unity",

        // Soft / transferable skills — still relevant across every IT role
        "communication", "teamwork", "leadership", "problem solving",
        "project management", "critical thinking", "time management",
    };

    private static readonly string[] Qualifications =
    {
        "BSc Computer Science", "BSc Information Systems", "BCom Informatics",
        "BSc Software Engineering", "BSc Information Technology", "BSc Data Science",
        "BSc Cybersecurity", "BEng Computer Engineering", "Bachelor's Degree",
        "Honours Degree", "Diploma in IT", "National Diploma", "Higher Certificate in IT",
        "Advanced Diploma in IT",
    };

    public static void Seed(IRepositoryWrapper repository)
    {
        var existing = repository.SkillDictionary.GetAll();
        var existingTerms = existing.Select(t => t.Term).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var s in Skills)
            if (!existingTerms.Contains(s))
                repository.SkillDictionary.Add(new SkillDictionaryTerm { Term = s, IsQualification = false });

        foreach (var q in Qualifications)
            if (!existingTerms.Contains(q))
                repository.SkillDictionary.Add(new SkillDictionaryTerm { Term = q, IsQualification = true });
    }
}
