# Skyline N-Mod Filter

Skyline N-Mod Filter is a Windows External Tool for Skyline-daily. It creates a new Skyline document containing only peptide targets whose `modified_sequence` contains the literal, case-sensitive substring `N[`.

## Verified compatibility

- Skyline-daily `26.1.1.097-922725ca01`
- Skyline-daily (64-bit) `26.1.1.209 (61fa751304)`
- `SkylineTool.dll` exposes report retrieval and live-document deletion, but not open/save-copy or settings mutation.
- `SkylineCmd.exe` exposes `--in`, `--out`, `--refine-min-peptides`, and `--pep-max-variable-mods`.
- The source document is never edited. XML deletion is restricted to a temporary copy, which SkylineCmd must reopen and normalize before publication.

## Installation

1. Download `SkylineNModFilter-1.2.0.zip` from the repository's `releases` folder.
2. Completely close Skyline-daily.
3. Reopen Skyline-daily and choose **Tools > External Tools**.
4. Remove an older **N-Mod Filter** entry if present.
5. Choose **Add > From File** and select the ZIP without extracting it.
6. Restart Skyline-daily once more before running the tool.

## What it does

- Keeps only peptides whose Skyline `modified_sequence` contains the literal, case-sensitive substring `N[`.
- Removes empty peptides, proteins, and protein groups.
- Sets maximum variable modifications to 1.
- Saves a separate `<original>_N-mod-filtered.sky` document and publishes its Skyline companion files under the matching basename.
- Sets Skyline's modification display to three-letter codes.
- Optionally associates peptides with proteins from an installed background proteome.

The source document is not overwritten.

## Protein association

Protein association is optional. When enabled, choose an installed background proteome in the tool dialog. N-Mod Filter opens the selected `.protdb` read-only, exports its distinct protein sequences to a temporary FASTA, and supplies that FASTA to SkylineCmd. The temporary FASTA is removed after SkylineCmd succeeds or fails; the installed background proteome is never modified.

Association creates protein groups for proteins matching the same peptides and assigns shared peptides using Skyline's `AssignedToBestProtein` policy.

## Building and testing

The project uses the .NET Framework C# compiler and PowerShell scripts. Place
the matching 64-bit `System.Data.SQLite.dll` and `SQLite.Interop.dll` files from
your Skyline-daily installation in `../work/SkylineRuntime`, relative to this
project directory. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Run-Tests.ps1
powershell -ExecutionPolicy Bypass -File scripts/Package-Tool.ps1
```

## License

Skyline N-Mod Filter is released under the MIT License. See `LICENSE` and
`THIRD_PARTY_NOTICES.md`.
