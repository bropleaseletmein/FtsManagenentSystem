const getToken = () => localStorage.getItem('admin_token')

async function request(method, url, body) {
  const headers = { 'Content-Type': 'application/json' }
  const token = getToken()
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(url, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (res.status === 204) return null
  const data = await res.json().catch(() => null)
  if (!res.ok) throw new Error(data?.error ?? data?.title ?? `HTTP ${res.status}`)

  // Handle PagedResult - extract items for list endpoints
  if (data?.items && Array.isArray(data.items) && data.total !== undefined) {
    return data.items
  }
  return data
}

export const api = {
  get:  (url)        => request('GET',    url),
  post: (url, body)  => request('POST',   url, body),
  put:  (url, body)  => request('PUT',    url, body),
  del:  (url)        => request('DELETE', url),
}
