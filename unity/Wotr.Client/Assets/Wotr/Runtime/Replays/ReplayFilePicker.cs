using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wotr.Client
{
    // REPLAY-03: where a replay comes from or goes to, behind an adapter so the library never depends on a platform.
    // Null means the player cancelled.
    public interface IReplayFilePicker
    {
        Task<string> ChooseImport();
        Task<string> ChooseExport(string suggestedName);
    }

    // The system's own dialogs without the editor's API: on macOS through AppleScript's choose file, on Windows through
    // the common file dialogs.
    public sealed class NativeReplayFilePicker : IReplayFilePicker
    {
        public Task<string> ChooseImport() => Application.platform switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => Task.FromResult(WindowsDialogs.Open()),
            _ => AppleScript("POSIX path of (choose file with prompt \"Import a replay\" of type {\"wotrreplay\"})")
        };

        public Task<string> ChooseExport(string suggestedName) => Application.platform switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => Task.FromResult(WindowsDialogs.Save(suggestedName)),
            _ => AppleScript("POSIX path of (choose file name with prompt \"Export the replay\" default name \"" +
                Quoted(suggestedName) + "\")")
        };

        private static string Quoted(string text) => text.Replace("\\", "").Replace("\"", "");

        private static Task<string> AppleScript(string script) => Task.Run(() =>
        {
            var start = new ProcessStartInfo("/usr/bin/osascript", "-e \"" + script.Replace("\"", "\\\"") + "\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(start);
            var chosen = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && chosen.Length > 0 ? chosen : null;
        });

        private static class WindowsDialogs
        {
            private const int NoChangeDirectory = 0x00000008;
            private const int PathMustExist = 0x00000800;
            private const int FileMustExist = 0x00001000;
            private const int OverwritePrompt = 0x00000002;
            private const string Filter = "Replays (*.wotrreplay)\0*.wotrreplay\0\0";

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct FileDialog
            {
                public int Size;
                public IntPtr Owner;
                public IntPtr Instance;
                public string Filter;
                public string CustomFilter;
                public int MaxCustomFilter;
                public int FilterIndex;
                public IntPtr File;
                public int MaxFile;
                public string FileTitle;
                public int MaxFileTitle;
                public string InitialDirectory;
                public string Title;
                public int Flags;
                public short FileOffset;
                public short FileExtension;
                public string DefaultExtension;
                public IntPtr CustomData;
                public IntPtr Hook;
                public string Template;
                public IntPtr Reserved;
                public int ReservedNumber;
                public int FlagsEx;
            }

            [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
            private static extern bool GetOpenFileName(ref FileDialog dialog);

            [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetSaveFileNameW")]
            private static extern bool GetSaveFileName(ref FileDialog dialog);

            public static string Open() => Show("Import a replay", "", FileMustExist | PathMustExist, GetOpenFileName);

            public static string Save(string suggestedName) => Show("Export the replay", suggestedName, OverwritePrompt, GetSaveFileName);

            private delegate bool Dialog(ref FileDialog dialog);

            private static string Show(string title, string initial, int flags, Dialog show)
            {
                const int capacity = 1024;
                var buffer = Marshal.AllocHGlobal(capacity * 2);
                try
                {
                    var name = Encoding.Unicode.GetBytes(initial + "\0");
                    Marshal.Copy(name, 0, buffer, Math.Min(name.Length, capacity * 2 - 2));
                    var dialog = new FileDialog
                    {
                        Size = Marshal.SizeOf<FileDialog>(), Filter = Filter, FilterIndex = 1, File = buffer, MaxFile = capacity,
                        Title = title, Flags = flags | NoChangeDirectory, DefaultExtension = "wotrreplay"
                    };
                    return show(ref dialog) ? Marshal.PtrToStringUni(buffer) : null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
