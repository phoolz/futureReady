namespace FutureReady.Models.StudentWorkHistory
{
    public class WorkHistoryFormDto
    {
        public List<CourseDto> CurrentCourses { get; set; } = new();
        public List<QualificationDto> VetQualifications { get; set; } = new();
        public CertificatesDto Certificates { get; set; } = new();
        public List<EmploymentDto> PartTimeEmployment { get; set; } = new();
        public List<CommunityServiceDto> CommunityService { get; set; } = new();
    }

    public class CourseDto
    {
        public string Name { get; set; } = "";
    }

    public class QualificationDto
    {
        public string Name { get; set; } = "";
    }

    public class EmploymentDto
    {
        public string EmployerName { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class CommunityServiceDto
    {
        public string Description { get; set; } = "";
    }

    public class CertificatesDto
    {
        // WorkSafe SmartMove Modules (16)
        public bool SmartMoveAutomotive { get; set; }
        public DateOnly? SmartMoveAutomotiveDate { get; set; }

        public bool SmartMoveBuildingConstruction { get; set; }
        public DateOnly? SmartMoveBuildingConstructionDate { get; set; }

        public bool SmartMoveBusinessIT { get; set; }
        public DateOnly? SmartMoveBusinessITDate { get; set; }

        public bool SmartMoveElectrical { get; set; }
        public DateOnly? SmartMoveElectricalDate { get; set; }

        public bool SmartMoveFarmingForestryFishing { get; set; }
        public DateOnly? SmartMoveFarmingForestryFishingDate { get; set; }

        public bool SmartMoveGeneralModule { get; set; }
        public DateOnly? SmartMoveGeneralModuleDate { get; set; }

        public bool SmartMoveHairdressing { get; set; }
        public DateOnly? SmartMoveHairdressingDate { get; set; }

        public bool SmartMoveHealthCommunityServices { get; set; }
        public DateOnly? SmartMoveHealthCommunityServicesDate { get; set; }

        public bool SmartMoveHospitalityTourism { get; set; }
        public DateOnly? SmartMoveHospitalityTourismDate { get; set; }

        public bool SmartMoveManufacturing { get; set; }
        public DateOnly? SmartMoveManufacturingDate { get; set; }

        public bool SmartMoveMetalsEngineering { get; set; }
        public DateOnly? SmartMoveMetalsEngineeringDate { get; set; }

        public bool SmartMoveMining { get; set; }
        public DateOnly? SmartMoveMiningDate { get; set; }

        public bool SmartMoveNailBeautyTechnology { get; set; }
        public DateOnly? SmartMoveNailBeautyTechnologyDate { get; set; }

        public bool SmartMoveRetail { get; set; }
        public DateOnly? SmartMoveRetailDate { get; set; }

        public bool SmartMoveSportRecreation { get; set; }
        public DateOnly? SmartMoveSportRecreationDate { get; set; }

        public bool SmartMoveWHSExtensionModule { get; set; }
        public DateOnly? SmartMoveWHSExtensionModuleDate { get; set; }

        // Other Certificates (9)
        public bool WhiteCard { get; set; }
        public DateOnly? WhiteCardDate { get; set; }

        public bool WorkSafeSafetyPassport { get; set; }
        public DateOnly? WorkSafeSafetyPassportDate { get; set; }

        public bool ElectricalTrainingLicence { get; set; }
        public DateOnly? ElectricalTrainingLicenceDate { get; set; }

        public bool FirstAidCertificate { get; set; }
        public string? FirstAidLevel { get; set; }
        public DateOnly? FirstAidCertificateDate { get; set; }

        public bool BronzeMedallion { get; set; }
        public DateOnly? BronzeMedallionDate { get; set; }

        public bool SchoolWorkplaceInduction { get; set; }
        public DateOnly? SchoolWorkplaceInductionDate { get; set; }

        public bool WorkplaceInductionProgram { get; set; }
        public DateOnly? WorkplaceInductionProgramDate { get; set; }

        public bool Year9_10WorkStudies { get; set; }
        public DateOnly? Year9_10WorkStudiesDate { get; set; }
    }
}
