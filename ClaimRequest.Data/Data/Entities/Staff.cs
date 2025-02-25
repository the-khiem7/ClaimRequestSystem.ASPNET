using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRequest.DAL.Data.Entities
{
    public enum SystemRole
    {
        ProjectManager,
        Staff,
        Finance,
        Admin,
        Spyware
    }

    public enum Department
    {
        SoftwareDevelopment,   // Developers, engineers, and coders
        QualityAssurance,      // Testers and QA engineers
        ITSupport,            // Helpdesk and infrastructure support
        ProjectManagement,    // Project managers and coordinators
        BusinessAnalysis,     // Business analysts and requirement gatherers
        UIUXDesign,           // UX/UI designers and front-end specialists
        DevOps,               // CI/CD, cloud engineers, and automation
        CyberSecurity,        // Security specialists and compliance officers
        DataScience,          // AI, machine learning, and big data analytics
        TechnicalWriting,     // Documentation and knowledge base maintenance
        HumanResources,       // Recruitment, employee relations, and training
        Finance,              // Budgeting, accounting, and financial planning
        SalesAndMarketing,    // Sales, marketing, and client relationship management
        CustomerSupport,      // Handling customer inquiries and support tickets
        ResearchAndInnovation,// Exploring new technologies and innovations
        LegalAndCompliance,    // Handling legal, contracts, and policy enforcement
        // Software Development
        SoftwareEngineer,
        SeniorSoftwareEngineer,
        TechLead,
        SoftwareArchitect,

        // Quality Assurance
        QAEngineer,
        SeniorQAEngineer,
        QAAnalyst,
        AutomationTester,

        // IT Support
        ITSupportSpecialist,
        HelpdeskTechnician,
        SystemAdministrator,

        // Project Management
        ProjectManager,
        ScrumMaster,
        AgileCoach,

        // Business Analysis
        BusinessAnalyst,
        ProductOwner,
        RequirementsAnalyst,

        // UI/UX Design
        UXDesigner,
        UIDesigner,
        FrontEndDeveloper,

        // DevOps
        DevOpsEngineer,
        CloudEngineer,
        SiteReliabilityEngineer,

        // Cyber Security
        SecurityAnalyst,
        EthicalHacker,
        ComplianceOfficer,

        // Data Science
        DataScientist,
        DataEngineer,
        MachineLearningEngineer,

        // Technical Writing
        TechnicalWriter,
        DocumentationSpecialist,

        // Human Resources
        HRManager,
        TalentAcquisitionSpecialist,
        EmployeeRelationsManager,

        // Finance
        FinancialAnalyst,
        Accountant,
        ChiefFinancialOfficer,

        // Sales & Marketing
        SalesRepresentative,
        DigitalMarketingManager,
        BrandStrategist,

        // Customer Support
        CustomerSupportRepresentative,
        TechnicalSupportSpecialist,

        // Research & Innovation
        ResearchScientist,
        InnovationManager,

        // Legal & Compliance
        LegalAdvisor,
        ComplianceManager

    }


    [Table("Staffs")]
    public class Staff
    {
        [Key]
        [Required]
        [Column("id")]
        public Guid Id { get; set; }
        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        [Column("email")]
        [StringLength(256)]
        public string Email { get; set; }
        [Required]
        [Column("password")]
        public string Password { get; set; }
        [Required]
        [Column("role")]
        public SystemRole SystemRole { get; set; }

        [Column("department")]
        [Required]
        public Department Department { get; set; }

        [Column("salary", TypeName = "numeric")]
        public decimal Salary { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ProjectStaff> ProjectStaffs { get; set; } = [];

        //public string PasswordHash { get; set; }
    }
}
