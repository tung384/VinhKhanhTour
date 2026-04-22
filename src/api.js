import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
});

const backendOrigin =
  import.meta.env.VITE_API_ORIGIN?.replace(/\/$/, '') ??
  (typeof window !== 'undefined' ? window.location.origin : '');

export const resolveMediaUrl = (url) => {
  if (!url) {
    return '';
  }

  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  if (url.startsWith('/')) {
    return `${backendOrigin}${url}`;
  }

  return `${backendOrigin}/${url.replace(/^\/+/, '')}`;
};

export const setAuthToken = (token) => {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`;
    return;
  }

  delete api.defaults.headers.common.Authorization;
};

export const authService = {
  login: (username, password) => api.post('/auth/login', { username, password }),
};

export const poiService = {
  getAll: () => api.get('/POIs'),
  create: (data) => api.post('/admin/poi', data),
  update: (id, data) => api.put(`/admin/poi/${id}`, data),
  delete: (id) => api.delete(`/admin/poi/${id}`),

  // CHANGE: upload images first, then store only the returned URL in MySQL.
  uploadImage: (file) => {
    const formData = new FormData();
    formData.append('file', file);

    return api.post('/admin/poi/upload-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  }
};

export const accountService = {
  getAll: () => api.get('/admin/accounts'),
  create: (data) => api.post('/admin/accounts', data),
  updateStatus: (id, isActive) => api.put(`/admin/accounts/${id}/status`, { isActive }),
  updatePassword: (id, password) => api.put(`/admin/accounts/${id}/password`, { password }),
  delete: (id) => api.delete(`/admin/accounts/${id}`),
};

export const ownerPoiService = {
  getMine: () => api.get('/owner/poi'),
  updateMine: (data) => api.put('/owner/poi', data),
  uploadImage: (file) => {
    const formData = new FormData();
    formData.append('file', file);

    return api.post('/owner/poi/upload-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  }
};
