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
}