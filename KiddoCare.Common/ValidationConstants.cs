namespace KiddoCare.Common;

public static class ValidationConstants
{
    //Groups
    public const int KindergartenGroupNameMaxLength = 80;
    public const int KindergartenGroupDescriptionMaxLength = 500;

    //Children
    public const int ChildFirstNameMaxLength = 50;
    public const int ChildLastNameMaxLength = 50;
    public const int ChildAllergiesMaxLength = 500;
    public const int ChildAdditionalNotesMaxLength = 1000;
    public const int ChildPhotoUrlMaxLength = 2048;

    //Attendance
    public const int AttendanceNoteMaxLength = 500;

    //Event
    public const int EventTitleMaxLength = 100;
    public const int EventDescriptionMaxLength = 1000;
    public const int EventLocationMaxLength = 200;

    //Parents
    public const int ParentFullNameMaxLength = 100;
    public const int ParentPhoneNumberMaxLength = 30;

    //Teachers
    public const int TeacherFullNameMaxLength = 100;
    public const int TeacherPhoneNumberMaxLength = 30;

    //Announcements
    public const int AnnouncementTitleMaxLength = 120;
    public const int AnnouncementContentMaxLength = 2000;

    //Daily Reports
    public const int DailyReportMealsMaxLength = 300;
    public const int DailyReportSleepMaxLength = 300;
    public const int DailyReportActivitiesMaxLength = 1000;
    public const int DailyReportTeacherNoteMaxLength = 1000;

    //Medical Records
    public const int MedicalRecordAllergiesMaxLength = 1000;
    public const int MedicalRecordChronicConditionsMaxLength = 1000;
    public const int MedicalRecordDoctorNameMaxLength = 100;
    public const int MedicalRecordDoctorPhoneMaxLength = 30;
    public const int MedicalRecordEmergencyContactNameMaxLength = 100;
    public const int MedicalRecordEmergencyContactPhoneMaxLength = 30;
    public const int MedicalRecordNotesMaxLength = 1000;
}