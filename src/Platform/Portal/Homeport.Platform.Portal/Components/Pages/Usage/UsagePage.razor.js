// SPDX-License-Identifier: AGPL-3.0-or-later

// Hands a download the circuit streamed to the browser to the operator as a
// file, without a second request: the export was read, and audited, once.
export async function download(fileName, stream) {
    const blob = new Blob([await stream.arrayBuffer()]);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
}
