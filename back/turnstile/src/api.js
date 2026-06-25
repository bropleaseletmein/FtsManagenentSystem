const getToken = () => localStorage.getItem('turnstile_token')

async function request(method, url, body) {
  const opts = { method, headers: { 'Content-Type': 'application/json' } }
  const token = getToken()
  if (token) opts.headers['Authorization'] = `Bearer ${token}`
  if (body !== undefined) opts.body = JSON.stringify(body)

  const res = await fetch(url, opts)
  if (res.status === 204) return null
  if (!res.ok) {
    const err = await res.json().catch(() => ({}))
    throw new Error(err.error || `HTTP ${res.status}`)
  }
  const text = await res.text()
  return text ? JSON.parse(text) : null
}

export const api = {
  get:  (url)       => request('GET',  url),
  post: (url, body) => request('POST', url, body),
}
