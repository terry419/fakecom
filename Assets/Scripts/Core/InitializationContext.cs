using System;
using System.Text;

public readonly struct ValidationResult
{
    public readonly bool IsValid;
    public readonly string ErrorMessage;

    public ValidationResult(bool isValid, string errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new ValidationResult(true);
    public static ValidationResult Failure(string message) => new ValidationResult(false, message);

    public static implicit operator bool(ValidationResult result) => result.IsValid;
}

public struct InitializationContext
{
    public ManagerScope Scope;
    public GlobalSettingsSO GlobalSettings;
    public TileRegistrySO Registry;

    // [Fix] 타입 변경 (MapCatalogManager -> MapCatalogSO)
    public MapCatalogSO MapCatalog;

    public MissionDataSO MissionData;
    public MapDataSO MapData;

    public ValidationResult Validate()
    {
        var errors = new StringBuilder();
        bool hasError = false;

        if (GlobalSettings == null) { errors.AppendLine("- GlobalSettings is Missing."); hasError = true; }
        if (Registry == null) { errors.AppendLine("- TileRegistry is Missing."); hasError = true; }

        if (Scope == ManagerScope.Scene)
        {
            // Scene에서는 데이터가 필수
            if (MissionData == null) { errors.AppendLine("- MissionData is Missing (Scene Scope)."); hasError = true; }
            if (MapData == null) { errors.AppendLine("- MapData is Missing (Scene Scope)."); hasError = true; }
        }
        else if (Scope == ManagerScope.Session)
        {
            // [Fix] Session Scope는 Empty Session(MissionData == null) 허용
            // 따라서 MissionData null 체크 제거
        }

        return hasError ? ValidationResult.Failure(errors.ToString()) : ValidationResult.Success();
    }
}