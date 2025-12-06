using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Logging;

namespace DellarteDellaGuerra.Infrastructure.Logging
{
    public class DadgDebugManager : IDebugManager
    {
        private readonly ILogger _logger;

        public DadgDebugManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DadgDebugManager>();
        }

        void IDebugManager.SetCrashReportCustomString(string customString)
        {
            // No direct logging, keep empty or log as info
            _logger.Info($"CrashReportCustomString set: {customString}");
        }

        void IDebugManager.SetCrashReportCustomStack(string customStack)
        {
            // No direct logging, keep empty or log as info
            _logger.Info($"CrashReportCustomStack set: {customStack}");
        }

        void IDebugManager.ShowWarning(string message)
        {
            _logger.Warn(message);
        }

        void IDebugManager.ShowError(string message)
        {
            _logger.Error(message);
        }

        void IDebugManager.ShowMessageBox(string lpText, string lpCaption, uint uType)
        {
            // Log message box info
            _logger.Info($"MessageBox shown - Caption: {lpCaption}, Text: {lpText}, Type: {uType}");
        }

        void IDebugManager.Assert(bool condition, string message, string callerFile, string callerMethod, int callerLine)
        {
            if (!condition)
            {
                _logger.Error($"Assert failed at {callerFile}:{callerLine} in {callerMethod} - {message}");
            }
        }

        void IDebugManager.SilentAssert(bool condition, string message, bool getDump, string callerFile, string callerMethod, int callerLine)
        {
            if (!condition)
            {
                _logger.Error($"SilentAssert failed at {callerFile}:{callerLine} in {callerMethod} - {message} (GetDump: {getDump})");
            }
        }

        void IDebugManager.Print(string message, int logLevel, Debug.DebugColor color, ulong debugFilter)
        {
            // Map logLevel to logger method
            // Assuming logLevel: 0=Debug, 1=Info, 2=Warn, 3=Error, 4=Fatal
            switch (logLevel)
            {
                case 0:
                    _logger.Debug(message);
                    break;
                case 1:
                    _logger.Info(message);
                    break;
                case 2:
                    _logger.Warn(message);
                    break;
                case 3:
                    _logger.Error(message);
                    break;
                case 4:
                    _logger.Fatal(message);
                    break;
                default:
                    _logger.Info(message);
                    break;
            }
        }

        void IDebugManager.PrintError(string error, string stackTrace, ulong debugFilter)
        {
            _logger.Error($"{error}\nStackTrace: {stackTrace}");
        }

        void IDebugManager.PrintWarning(string warning, ulong debugFilter)
        {
            _logger.Warn(warning);
        }

        void IDebugManager.DisplayDebugMessage(string message)
        {
            _logger.Debug(message);
        }

        void IDebugManager.WatchVariable(string name, object value)
        {
            _logger.Debug($"WatchVariable - {name}: {value}");
        }

        void IDebugManager.WriteDebugLineOnScreen(string message)
        {
            _logger.Debug($"DebugLineOnScreen: {message}");
        }

        void IDebugManager.RenderDebugLine(Vec3 position, Vec3 direction, uint color, bool depthCheck, float time)
        {
            // No direct logging, can log debug info if needed
            _logger.Debug($"RenderDebugLine - Position: {position}, Direction: {direction}, Color: {color}, DepthCheck: {depthCheck}, Time: {time}");
        }

        void IDebugManager.RenderDebugSphere(Vec3 position, float radius, uint color, bool depthCheck, float time)
        {
            _logger.Debug($"RenderDebugSphere - Position: {position}, Radius: {radius}, Color: {color}, DepthCheck: {depthCheck}, Time: {time}");
        }

        void IDebugManager.RenderDebugFrame(MatrixFrame frame, float lineLength, float time)
        {
            _logger.Debug($"RenderDebugFrame - Frame: {frame}, LineLength: {lineLength}, Time: {time}");
        }

        void IDebugManager.RenderDebugText(float screenX, float screenY, string text, uint color, float time)
        {
            _logger.Debug($"RenderDebugText - ScreenX: {screenX}, ScreenY: {screenY}, Text: {text}, Color: {color}, Time: {time}");
        }

        void IDebugManager.RenderDebugText3D(Vec3 position, string text, uint color, int screenPosOffsetX, int screenPosOffsetY, float time)
        {
            _logger.Debug($"RenderDebugText3D - Position: {position}, Text: {text}, Color: {color}, OffsetX: {screenPosOffsetX}, OffsetY: {screenPosOffsetY}, Time: {time}");
        }

        void IDebugManager.RenderDebugRectWithColor(float left, float bottom, float right, float top, uint color)
        {
            _logger.Debug($"RenderDebugRectWithColor - Left: {left}, Bottom: {bottom}, Right: {right}, Top: {top}, Color: {color}");
        }

        Vec3 IDebugManager.GetDebugVector()
        {
            return MBDebug.DebugVector;
        }

        void IDebugManager.SetDebugVector(Vec3 value)
        {
            MBDebug.DebugVector = value;
        }

        void IDebugManager.SetTestModeEnabled(bool testModeEnabled)
        {
            MBDebug.TestModeEnabled = testModeEnabled;
            _logger.Info($"TestModeEnabled set to {testModeEnabled}");
        }

        void IDebugManager.AbortGame()
        {
            _logger.Fatal("Game aborted.");
            MBDebug.AbortGame();
        }

        void IDebugManager.DoDelayedexit(int returnCode)
        {
            _logger.Info($"DoDelayedexit called with returnCode: {returnCode}");
            Utilities.DoDelayedexit(returnCode);
        }

        void IDebugManager.ReportMemoryBookmark(string message)
        {
            _logger.Info($"MemoryBookmark: {message}");
        }
    }
}