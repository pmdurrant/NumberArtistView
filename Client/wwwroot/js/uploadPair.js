// JavaScript (browser) -- call when user selects two files
async function uploadPair(dxfFile, jpgFile, token) {
  const url = '/api/DxfFiles/UploadPair';
  const form = new FormData();
  form.append('file1', dxfFile); // MUST be .dxf
  form.append('file2', jpgFile); // MUST be .jpg

  const res = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
      // DO NOT set Content-Type; browser sets multipart boundary
    },
    body: form
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`UploadPair failed: ${res.status} ${text}`);
  }

  return res.json();
}