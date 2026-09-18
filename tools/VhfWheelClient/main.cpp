#include <windows.h>
#include <setupapi.h>

#include <cerrno>
#include <climits>
#include <cstdint>
#include <cwchar>
#include <iostream>
#include <string>
#include <vector>

#include "..\..\driver\VhfWheelTest\VhfWheelTestProtocol.h"

#pragma comment(lib, "setupapi.lib")

constexpr GUID GUID_DEVINTERFACE_VHF_WHEEL_TEST{
    0xb44e70a5,
    0xf238,
    0x49ba,
    {0x94, 0x0c, 0x09, 0x34, 0x86, 0x0e, 0x91, 0xe1}};

static_assert(sizeof(VHF_WHEEL_TEST_REQUEST) == 20);

namespace
{
constexpr long kMaximumReportCount = 1000;
constexpr long kMaximumIntervalMilliseconds = 1000;

bool TryParseLong(const wchar_t* text, long minimum, long maximum, long& value)
{
    wchar_t* end = nullptr;
    errno = 0;
    const long parsed = std::wcstol(text, &end, 10);
    if (errno == ERANGE || end == text || *end != L'\0' ||
        parsed < minimum || parsed > maximum)
    {
        return false;
    }

    value = parsed;
    return true;
}

std::wstring FormatWindowsError(DWORD error)
{
    wchar_t* message = nullptr;
    const DWORD length = FormatMessageW(
        FORMAT_MESSAGE_ALLOCATE_BUFFER |
            FORMAT_MESSAGE_FROM_SYSTEM |
            FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr,
        error,
        0,
        reinterpret_cast<wchar_t*>(&message),
        0,
        nullptr);

    std::wstring result = length == 0 ? L"Unknown error" : std::wstring(message, length);
    if (message != nullptr)
    {
        LocalFree(message);
    }
    return result;
}

HANDLE OpenTestDevice()
{
    HDEVINFO deviceInfo = SetupDiGetClassDevsW(
        &GUID_DEVINTERFACE_VHF_WHEEL_TEST,
        nullptr,
        nullptr,
        DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);
    if (deviceInfo == INVALID_HANDLE_VALUE)
    {
        return INVALID_HANDLE_VALUE;
    }

    SP_DEVICE_INTERFACE_DATA interfaceData{};
    interfaceData.cbSize = sizeof(interfaceData);
    if (!SetupDiEnumDeviceInterfaces(
            deviceInfo,
            nullptr,
            &GUID_DEVINTERFACE_VHF_WHEEL_TEST,
            0,
            &interfaceData))
    {
        const DWORD error = GetLastError();
        SetupDiDestroyDeviceInfoList(deviceInfo);
        SetLastError(error);
        return INVALID_HANDLE_VALUE;
    }

    DWORD requiredSize = 0;
    SetupDiGetDeviceInterfaceDetailW(
        deviceInfo,
        &interfaceData,
        nullptr,
        0,
        &requiredSize,
        nullptr);
    if (requiredSize < sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W))
    {
        const DWORD error = GetLastError();
        SetupDiDestroyDeviceInfoList(deviceInfo);
        SetLastError(error == ERROR_SUCCESS ? ERROR_INVALID_DATA : error);
        return INVALID_HANDLE_VALUE;
    }

    std::vector<std::uint8_t> detailBuffer(requiredSize);
    auto* detail = reinterpret_cast<SP_DEVICE_INTERFACE_DETAIL_DATA_W*>(
        detailBuffer.data());
    detail->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W);

    if (!SetupDiGetDeviceInterfaceDetailW(
            deviceInfo,
            &interfaceData,
            detail,
            requiredSize,
            nullptr,
            nullptr))
    {
        const DWORD error = GetLastError();
        SetupDiDestroyDeviceInfoList(deviceInfo);
        SetLastError(error);
        return INVALID_HANDLE_VALUE;
    }

    const HANDLE device = CreateFileW(
        detail->DevicePath,
        GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);
    const DWORD error = GetLastError();
    SetupDiDestroyDeviceInfoList(deviceInfo);
    SetLastError(error);
    return device;
}

void PrintUsage()
{
    std::wcerr
        << L"Usage: VhfWheelClient.exe <vertical -127..127> "
           L"<horizontal -127..127> [count 1..1000] [interval-ms 0..1000]\n"
        << L"At least one delta must be non-zero. Positive/negative direction "
           L"follows Windows HID wheel conventions.\n";
}
}

int wmain(int argc, wchar_t* argv[])
{
    if (argc < 3 || argc > 5)
    {
        PrintUsage();
        return 2;
    }

    long vertical = 0;
    long horizontal = 0;
    long count = 1;
    long intervalMilliseconds = 0;
    if (!TryParseLong(
            argv[1],
            VHF_WHEEL_TEST_DELTA_MIN,
            VHF_WHEEL_TEST_DELTA_MAX,
            vertical) ||
        !TryParseLong(
            argv[2],
            VHF_WHEEL_TEST_DELTA_MIN,
            VHF_WHEEL_TEST_DELTA_MAX,
            horizontal) ||
        (argc >= 4 && !TryParseLong(argv[3], 1, kMaximumReportCount, count)) ||
        (argc >= 5 &&
         !TryParseLong(
             argv[4],
             0,
             kMaximumIntervalMilliseconds,
             intervalMilliseconds)) ||
        (vertical == 0 && horizontal == 0))
    {
        PrintUsage();
        return 2;
    }

    const HANDLE device = OpenTestDevice();
    if (device == INVALID_HANDLE_VALUE)
    {
        const DWORD error = GetLastError();
        std::wcerr << L"Unable to open VhfWheelTest device (" << error << L"): "
                   << FormatWindowsError(error);
        return 1;
    }

    const VHF_WHEEL_TEST_REQUEST request{
        sizeof(VHF_WHEEL_TEST_REQUEST),
        VHF_WHEEL_TEST_PROTOCOL_VERSION,
        vertical,
        horizontal,
        0};

    for (long index = 0; index < count; ++index)
    {
        DWORD bytesReturned = 0;
        if (!DeviceIoControl(
                device,
                IOCTL_VHF_WHEEL_TEST_INJECT,
                const_cast<VHF_WHEEL_TEST_REQUEST*>(&request),
                sizeof(request),
                nullptr,
                0,
                &bytesReturned,
                nullptr))
        {
            const DWORD error = GetLastError();
            CloseHandle(device);
            std::wcerr << L"Report " << (index + 1) << L" failed (" << error
                       << L"): " << FormatWindowsError(error);
            return 1;
        }

        if (intervalMilliseconds != 0 && index + 1 < count)
        {
            Sleep(static_cast<DWORD>(intervalMilliseconds));
        }
    }

    CloseHandle(device);
    std::wcout << L"Submitted " << count << L" report(s): vertical=" << vertical
               << L", horizontal=" << horizontal << L".\n";
    return 0;
}
