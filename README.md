# Skyline N-Mod Filter

Skyline N-Mod Filter is a Windows External Tool for Skyline-daily. It creates a new Skyline document containing only peptide targets whose `modified_sequence` contains the literal, case-sensitive substring `N[`.

## Verified compatibility

- Skyline-daily `26.1.1.097-922725ca01`
- Skyline-daily (64-bit) `26.1.1.209 (61fa751304)`
- `SkylineTool.dll` exposes report retrieval and live-document deletion, but not open/save-copy or settings mutation.
- `SkylineCmd.exe` exposes `--in`, `--out`, `--refine-min-peptides`, and `--pep-max-variable-mods`.
- The source document is never edited. XML deletion is restricted to a temporary copy, which SkylineCmd must reopen and normalize before publication.

## Installation

1. Download `SkylineNModFilter-1.6.0.zip` from the repository's `releases` folder.
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
- Optionally reorders results replicates from the row order of a FragPipe manifest, TSV, or CSV metadata file.
- Optionally treats the first nonblank metadata row as a header and uses its labels for rename-column selection.
- Optionally renames matched replicates from a selected metadata column while preserving raw-file and acquisition metadata.
- Optionally imports every metadata column after the filename key as a Skyline replicate annotation.
- Optionally removes precursor charge states whose precursor-level peak area is missing in more than a selected percentage of all replicates.
- Supports missingness-only output and missingness filtering within a selected metadata group or any metadata group.

The source document is not overwritten.

## Protein association

Protein association is optional. When enabled, choose an installed background proteome in the tool dialog. N-Mod Filter opens the selected `.protdb` read-only, exports its distinct protein sequences to a temporary FASTA, and supplies that FASTA to SkylineCmd. The temporary FASTA is removed after SkylineCmd succeeds or fails; the installed background proteome is never modified.

Association creates protein groups for proteins matching the same peptides and assigns shared peptides using Skyline's `AssignedToBestProtein` policy.

## Replicate ordering and renaming

Enable **Reorder replicates from metadata file** in the tool dialog and select a `.fp-manifest`, `.tsv`, or `.csv` file. Column 1 is matched case-insensitively to the original Skyline replicate name after removing directories and a terminal `.raw`. Matched replicates follow file row order; Skyline replicates absent from the file remain last in their existing order.

Enable **File contains header row** when appropriate. To rename, enable **Rename matched replicates** and select a column of 2 or greater. Blank rename values keep the original name. Duplicate final names stop processing before the output is published.

## Replicate annotations and volcano plots

Enable **Import all metadata columns as replicate annotations** to make metadata available in Skyline's Document Grid and Group Comparisons. A header row is required. Column 1 is the filename key; every nonblank, uniquely named column from column 2 onward becomes a text annotation that applies to replicates. Existing annotation definitions are preserved when they already apply to replicates. An existing definition with the same name that does not apply to replicates stops processing safely.

Matching is case-insensitive and removes directories and a terminal `.raw`. When ordering and renaming are enabled in the same run, annotation values are still matched from the original raw-file identity and written to the final replicate name. Skyline replicates missing from the metadata remain unannotated, and metadata rows missing from Skyline are reported. If one Skyline replicate contains multiple raw files, identical metadata values are accepted; conflicting values stop processing with an explanatory error.

After opening the output document, use **Settings > Document Settings > Annotations** to confirm the imported fields. A categorical annotation such as `Condition` can then be selected in Skyline's **Group Comparisons** to create a comparison and display Skyline's built-in volcano plot; MSstats is not required for that plot.

The equivalent command-line flag is `--import-replicate-annotations`, used with `--replicate-manifest <file>` and `--manifest-has-header`.

## Precursor peak-area missingness

Enable **Filter precursors by peak-area missingness** and set **Maximum missing data (%)** from 0 through 100; the default is 50. Every results replicate in the document is included in the denominator. A precursor is present in a replicate only when its Skyline `<precursor_peak>` has a finite numeric `area` greater than zero. Missing results, blank or invalid areas, zero, and negative areas count as missing.

Each precursor charge state is evaluated independently. A precursor exactly at the selected limit is retained; only precursors above the limit are removed. Empty peptides, proteins, and protein groups are removed afterward.

Equivalent command-line options are `--filter-precursor-missingness --max-missing-percent 50`.

### Missingness-only mode

Enable **Missingness-only mode** to skip the `N[` peptide-sequence filter and apply only precursor missingness filtering. Maximum variable modifications is still set to 1, and empty peptides and proteins are still removed. The output is named `<source>_missingness-filtered.sky`. Rerun from the same pre-missingness source when revising the threshold because previously removed precursors cannot be restored.

### Metadata group scopes

The **Missingness scope** choices are:

- **All replicates**: use every document replicate as one denominator.
- **Selected group**: use only replicates assigned to the chosen metadata group.
- **Any group**: retain a precursor when it meets the threshold in at least one group; remove it only when it fails in every evaluated group.

Selected-group and any-group modes use the same metadata file and header setting as optional replicate ordering. Column 1 matches Skyline replicate names, and **Group column** selects the metadata annotation. Group labels are case-insensitive. Blank group values and Skyline replicates absent from the metadata are assigned to **Unannotated**. Enable **Exclude unannotated replicates** to omit that group.

Grouped command-line options are `--missingness-scope selected|any`, `--replicate-manifest <file>`, `--group-column <number>`, optional `--selected-group <name>`, and optional `--exclude-unannotated`. Add `--missingness-only` for missingness-only output.

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
