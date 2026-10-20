using CareerSyncAI.Models;
using CareerSyncAI.Models.Enums;

namespace CareerSyncAI.Data;

public static class SeedLearningResources
{
    public static void Seed(IRepositoryWrapper repository)
    {
        if (repository.LearningResource.GetAll().Count > 0) return;

        var resources = new List<LearningResource>
        {
           
            // Programming languages
            
            New("Python", "Python for Everybody", "Coursera", 20, "https://coursera.org", LearningResourceType.Course),
            New("Python", "PCEP – Certified Entry-Level Python Programmer", "Python Institute", 15, "https://pythoninstitute.org", LearningResourceType.Certification),
            New("Java", "Java Programming Masterclass", "Udemy", 40, "https://udemy.com", LearningResourceType.Course),
            New("Java", "Oracle Certified Professional: Java SE Programmer", "Oracle", 25, "https://education.oracle.com/certification", LearningResourceType.Certification),
            New("C#", "C# Fundamentals", "Microsoft Learn", 8, "https://learn.microsoft.com", LearningResourceType.Course),
            New("JavaScript", "JavaScript Algorithms and Data Structures", "freeCodeCamp", 30, "https://freecodecamp.org", LearningResourceType.Course),
            New("JavaScript", "freeCodeCamp JavaScript Algorithms and Data Structures Certification", "freeCodeCamp", 30, "https://freecodecamp.org/certification", LearningResourceType.Certification),

            // ---------------------------------------------------------
            // Web development
            // ---------------------------------------------------------
            New("ASP.NET Core", "Build Web Apps with ASP.NET Core", "Microsoft Learn", 10, "https://learn.microsoft.com", LearningResourceType.Course),
            New("React", "React – The Complete Guide", "Udemy", 48, "https://udemy.com", LearningResourceType.Course),
            New("REST API", "REST API Design Best Practices", "Pluralsight", 4, "https://pluralsight.com", LearningResourceType.Course),
            New("Node.js", "The Complete Node.js Developer Course", "Udemy", 35, "https://udemy.com", LearningResourceType.Course),
            New("HTML", "Responsive Web Design", "freeCodeCamp", 15, "https://freecodecamp.org", LearningResourceType.Course),
            New("HTML", "freeCodeCamp Responsive Web Design Certification", "freeCodeCamp", 15, "https://freecodecamp.org/certification", LearningResourceType.Certification),

    
            // Databases
            
            New("SQL Server", "SQL Server Fundamentals", "Microsoft Learn", 4, "https://learn.microsoft.com", LearningResourceType.Course),
            New("SQL Server", "Microsoft Certified: Azure Database Administrator Associate", "Microsoft", 20, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("SQL", "SQL for Data Analysis", "Coursera", 12, "https://coursera.org", LearningResourceType.Course),
            New("MongoDB", "MongoDB Basics", "MongoDB University", 10, "https://university.mongodb.com", LearningResourceType.Course),
            New("MongoDB", "MongoDB Certified Developer Associate", "MongoDB", 15, "https://university.mongodb.com/certification", LearningResourceType.Certification),

            
            // Cloud & DevOps — expanded, cloud engineering emphasised
      
            New("Azure", "Azure Fundamentals", "Microsoft Learn", 6, "https://learn.microsoft.com", LearningResourceType.Course),
            New("Azure", "Microsoft Certified: Azure Fundamentals (AZ-900)", "Microsoft", 10, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("Azure", "Microsoft Certified: Azure Administrator Associate (AZ-104)", "Microsoft", 30, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("Microsoft Azure Administrator", "Microsoft Certified: Azure Administrator Associate (AZ-104)", "Microsoft", 30, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("AWS", "AWS Cloud Practitioner Essentials", "AWS Skill Builder", 6, "https://skillbuilder.aws", LearningResourceType.Course),
            New("AWS", "AWS Certified Cloud Practitioner", "Amazon Web Services", 15, "https://aws.amazon.com/certification", LearningResourceType.Certification),
            New("AWS", "AWS Certified Solutions Architect – Associate", "Amazon Web Services", 40, "https://aws.amazon.com/certification", LearningResourceType.Certification),
            New("Google Cloud Platform", "Google Cloud Digital Leader Training", "Google Cloud", 10, "https://cloud.google.com/learn", LearningResourceType.Course),
            New("Google Cloud Platform", "Google Associate Cloud Engineer", "Google Cloud", 30, "https://cloud.google.com/certification", LearningResourceType.Certification),
            New("Cloud Computing", "Cloud Computing Fundamentals", "Coursera", 15, "https://coursera.org", LearningResourceType.Course),
            New("Cloud Engineering", "Google Associate Cloud Engineer", "Google Cloud", 30, "https://cloud.google.com/certification", LearningResourceType.Certification),
            New("Cloud Architecture", "AWS Certified Solutions Architect – Associate", "Amazon Web Services", 40, "https://aws.amazon.com/certification", LearningResourceType.Certification),
            New("Docker", "Docker for Developers", "Udemy", 10, "https://udemy.com", LearningResourceType.Course),
            New("Docker", "Docker Certified Associate", "Docker", 12, "https://docker.com/certification", LearningResourceType.Certification),
            New("Kubernetes", "Kubernetes Basics", "Kubernetes.io", 8, "https://kubernetes.io/training", LearningResourceType.Course),
            New("Kubernetes", "Certified Kubernetes Administrator (CKA)", "Cloud Native Computing Foundation", 30, "https://cncf.io/certification/cka", LearningResourceType.Certification),
            New("DevOps", "DevOps Fundamentals", "Microsoft Learn", 8, "https://learn.microsoft.com", LearningResourceType.Course),
            New("Terraform", "HashiCorp Certified: Terraform Associate", "HashiCorp", 20, "https://hashicorp.com/certification", LearningResourceType.Certification),
            New("Infrastructure as Code", "HashiCorp Certified: Terraform Associate", "HashiCorp", 20, "https://hashicorp.com/certification", LearningResourceType.Certification),
            New("Site Reliability Engineering", "Google Cloud SRE Fundamentals", "Google Cloud", 10, "https://cloud.google.com/learn", LearningResourceType.Course),

            // Data science & AI
 
            New("Data Analysis", "Google Data Analytics Professional Certificate", "Google (via Coursera)", 60, "https://coursera.org", LearningResourceType.Certification),
            New("Data Science", "IBM Data Science Professional Certificate", "IBM (via Coursera)", 80, "https://coursera.org", LearningResourceType.Certification),
            New("Machine Learning", "Machine Learning Specialization", "Coursera (Andrew Ng / DeepLearning.AI)", 60, "https://coursera.org", LearningResourceType.Course),
            New("TensorFlow", "TensorFlow Developer Certificate", "Google / TensorFlow", 40, "https://tensorflow.org/certificate", LearningResourceType.Certification),
            New("Excel", "Excel for Data Analysis", "Coursera", 5, "https://coursera.org", LearningResourceType.Course),
            New("Excel", "Microsoft Office Specialist: Excel Associate", "Microsoft", 10, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("PowerBI", "Power BI Essentials", "Microsoft Learn", 3.5m, "https://learn.microsoft.com", LearningResourceType.Course),
            New("PowerBI", "Microsoft Certified: Power BI Data Analyst Associate", "Microsoft", 15, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("Tableau", "Tableau Desktop Specialist", "Tableau", 12, "https://tableau.com/learn/certification", LearningResourceType.Certification),

 
            // Cybersecurity — expanded, entry-level through specialised
 
            New("Cybersecurity", "Introduction to Cybersecurity", "Cisco Networking Academy", 6, "https://netacad.com", LearningResourceType.Course),
            New("Cybersecurity", "CompTIA Security+", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),
            New("Information Security", "ISC2 Certified in Cybersecurity (CC)", "ISC2", 20, "https://isc2.org/certifications/cc", LearningResourceType.Certification),
            New("Ethical Hacking", "Certified Ethical Hacker (CEH)", "EC-Council", 40, "https://eccouncil.org", LearningResourceType.Certification),
            New("Penetration Testing", "Practical lab practice (TryHackMe / HackTheBox)", "TryHackMe", 30, "https://tryhackme.com", LearningResourceType.Course),
            New("Penetration Testing", "CompTIA PenTest+", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),
            New("Network Security", "Network Security Fundamentals", "Cisco Networking Academy", 10, "https://netacad.com", LearningResourceType.Course),
            New("Security Operations", "CompTIA CySA+ (Cybersecurity Analyst)", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),
            New("SIEM", "SIEM Fundamentals", "Cisco Networking Academy", 8, "https://netacad.com", LearningResourceType.Course),
            New("Identity and Access Management", "Microsoft Identity and Access Administrator (SC-300)", "Microsoft", 20, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("Cloud Security", "Certificate of Cloud Security Knowledge (CCSK)", "Cloud Security Alliance", 20, "https://cloudsecurityalliance.org/certifications", LearningResourceType.Certification),
            New("Digital Forensics", "Digital Forensics Fundamentals", "Coursera", 15, "https://coursera.org", LearningResourceType.Course),
            New("Risk Management", "CompTIA Security+", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),
            New("Vulnerability Assessment", "CompTIA PenTest+", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),

        
            // Networking & systems
 
            New("networking", "Networking Basics", "Cisco Networking Academy", 20, "https://netacad.com", LearningResourceType.Course),
            New("networking", "CompTIA Network+", "CompTIA", 25, "https://comptia.org", LearningResourceType.Certification),
            New("Cisco", "Cisco Certified Network Associate (CCNA)", "Cisco", 40, "https://cisco.com/certifications", LearningResourceType.Certification),
            New("Windows Server", "Windows Server Administration", "Pluralsight", 6, "https://pluralsight.com", LearningResourceType.Course),
            New("Windows Server", "Microsoft Certified: Windows Server Hybrid Administrator Associate", "Microsoft", 20, "https://learn.microsoft.com/certifications", LearningResourceType.Certification),
            New("Linux", "Linux Command Line Basics", "freeCodeCamp", 8, "https://freecodecamp.org", LearningResourceType.Course),
            New("Linux", "CompTIA Linux+", "CompTIA", 25, "https://comptia.org", LearningResourceType.Certification),
            New("IT Support", "Google IT Support Professional Certificate", "Google (via Coursera)", 50, "https://coursera.org", LearningResourceType.Certification),
            New("IT Support", "CompTIA A+", "CompTIA", 30, "https://comptia.org", LearningResourceType.Certification),


            // Software engineering practice

            New("Git", "Git & Team Workflows", "freeCodeCamp", 3, "https://freecodecamp.org", LearningResourceType.Course),
            New("Unit Testing", "Unit Testing in .NET with xUnit", "Pluralsight", 2.5m, "https://pluralsight.com", LearningResourceType.Course),
            New("Agile", "Agile Fundamentals", "Coursera", 8, "https://coursera.org", LearningResourceType.Course),
            New("Scrum", "Certified ScrumMaster (CSM)", "Scrum Alliance", 16, "https://scrumalliance.org", LearningResourceType.Certification),
            New("project management", "Google Project Management Professional Certificate", "Google (via Coursera)", 45, "https://coursera.org", LearningResourceType.Certification),

    
            // Mobile & game development
        
            New("Android Development", "Android Basics with Compose", "Google (Android Developers)", 25, "https://developer.android.com/courses", LearningResourceType.Course),
            New("iOS Development", "iOS App Development with Swift", "Coursera", 30, "https://coursera.org", LearningResourceType.Course),
            New("Unity", "Unity Certified Associate: Game Developer", "Unity Technologies", 20, "https://unity.com/products/unity-certifications", LearningResourceType.Certification),
        };

        foreach (var r in resources)
            repository.LearningResource.Add(r);
    }

    private static LearningResource New(string skillName, string title, string provider, decimal durationHours, string url, LearningResourceType type) =>
        new()
        {
            SkillName = skillName,
            Title = title,
            Provider = provider,
            DurationHours = durationHours,
            Url = url,
            Type = type,
        };
}
