const defaultApiBaseUrl = 'http://localhost:5080'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? defaultApiBaseUrl
