# EPC-4 SerialNumber normalization — new TDD cycle

This document records a **new real TDD cycle executed now** in branch:
`feature/EPC-4-serial-normalization-tdd`.

## New rule
During equipment registration, `SerialNumber` must be normalized by trimming only leading and trailing spaces.

- Keep letter case unchanged.
- Keep inner spaces unchanged.
- Check uniqueness using normalized value.
- Save normalized value.
- Maximum length validation applies to normalized value.
- Empty/whitespace-only serial number remains invalid.

## Preparation
Commands:

```powershell
git -C "C:\visual project\EquipmentMonitoring" branch --show-current
git -C "C:\visual project\EquipmentMonitoring" status --short
git -C "C:\visual project\EquipmentMonitoring" fetch origin
git -C "C:\visual project\EquipmentMonitoring" checkout master
git -C "C:\visual project\EquipmentMonitoring" pull --ff-only origin master
git -C "C:\visual project\EquipmentMonitoring" checkout -b feature/EPC-4-serial-normalization-tdd
dotnet test "C:\visual project\EquipmentMonitoring\EquipmentMonitoring.slnx"
```

Initial result:
- `309 passed, 0 failed`.

## RED
Added failing tests (public API/validator paths, no production change):

- `EquipmentRegistrationValidatorTests.ValidateAsync_WhenSerialNumberContainsOuterSpaces_ShouldPassNormalizedSerialToExistsCheck`
- `EquipmentRegistrationValidatorTests.ValidateAsync_WhenNormalizedSerialAlreadyExists_ShouldReturnSerialNumberError`
- `EquipmentControllerTests.Post_WithSerialNumberOuterSpaces_ShouldReturn201AndPersistNormalizedSerialNumber`
- `EquipmentControllerTests.Post_WhenNormalizedSerialAlreadyExists_ShouldReturn400AndNotPersist`

Run result:
- `313 total, 309 passed, 4 failed`.

Actual failure reason (behavioral, expected in RED):
- `Assert.Equal() Failure: Strings differ`
- Expected: `"SN-TDD-001"`
- Actual: `"  SN-TDD-001  "`

So tests failed exactly because serial normalization was missing.

RED commit:
- `test(EPC-4): add failing serial normalization tests`

## GREEN
Minimal implementation:
- File: `EquipmentMonitoring.Application/Validation/EquipmentRegistrationValidator.cs`
- Added line before validation/uniqueness check:
  - `dto.SerialNumber = dto.SerialNumber?.Trim();`

This ensures:
- normalized value goes into validation (including max length),
- normalized value is used in uniqueness check,
- normalized value is later saved/returned.

Validation:

```powershell
dotnet build
# and full tests
```

Result:
- Build successful.
- `313 passed, 0 failed`.

GREEN commit:
- `feat(EPC-4): normalize serial numbers before registration`

## REFACTOR
Refactor not required.

Reason:
- behavior fixed by one minimal focused change,
- no duplication or structural debt introduced,
- all tests green.

No refactor commit created.
