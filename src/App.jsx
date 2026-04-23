import React, { useEffect, useMemo, useState } from 'react';
import {
  accountService,
  authService,
  deviceService,
  ownerPoiService,
  poiService,
  setAuthToken,
} from './api';
import AccountsPanel from './components/AccountsPanel';
import POIForm from './components/POIForm';
import POITable from './components/POITable';
import './App.css';
import { resolveMediaUrl } from './api';

const SESSION_KEY = 'cms_session_v1';

const loadStoredSession = () => {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

const formatDashboardDateTime = (value) => {
  if (!value) {
    return 'N/A';
  }

  const normalizedValue = /z$/i.test(value) ? value : `${value}Z`;
  const parsed = new Date(normalizedValue);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString('vi-VN', {
    hour12: false,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
};

function App() {
  const [session, setSession] = useState(loadStoredSession);
  const [loginForm, setLoginForm] = useState({ username: '', password: '' });
  const [loginLoading, setLoginLoading] = useState(false);

  const [currentTab, setCurrentTab] = useState('poi-list');
  const [pois, setPois] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingPoi, setEditingPoi] = useState(null);
  const [saving, setSaving] = useState(false);
  const [ownerPoi, setOwnerPoi] = useState(null);
  const [deviceDashboard, setDeviceDashboard] = useState({ totalDevices: 0, onlineDevices: 0, devices: [], topPois: [] });

  const isAdmin = session?.role === 'Admin';
  const isOwner = session?.role === 'Owner';
  const pageTitle = useMemo(() => (isAdmin ? 'ADMIN CMS' : 'STALL OWNER'), [isAdmin]);

  useEffect(() => {
    setAuthToken(session?.token ?? null);

    if (session) {
      localStorage.setItem(SESSION_KEY, JSON.stringify(session));
      return;
    }

    localStorage.removeItem(SESSION_KEY);
  }, [session]);

  useEffect(() => {
    if (isAdmin) {
      loadPOIs();
      loadAccounts();
    }

    if (isOwner) {
      loadOwnerPoi();
    }
  }, [isAdmin, isOwner]);

  useEffect(() => {
    if (isAdmin && currentTab === 'devices') {
      loadDeviceDashboard();
    }
  }, [isAdmin, currentTab]);

  const handleApiFailure = (error) => {
    const status = error?.response?.status;
    const code = error?.response?.data?.code;
    const message = error?.response?.data?.message;

    if (status === 403 && code === 'ACCOUNT_INACTIVE') {
      alert(message || 'You must pay before continue access the page.');
      handleLogout();
      return true;
    }

    if (status === 401) {
      alert('Session expired. Please login again.');
      handleLogout();
      return true;
    }

    return false;
  };

  const handleLogout = () => {
    setSession(null);
    setCurrentTab('poi-list');
    setShowForm(false);
    setEditingPoi(null);
    setOwnerPoi(null);
    setPois([]);
    setAccounts([]);
    setDeviceDashboard({ totalDevices: 0, onlineDevices: 0, devices: [], topPois: [] });
  };

  const handleLogin = async (event) => {
    event.preventDefault();
    setLoginLoading(true);

    try {
      const response = await authService.login(loginForm.username, loginForm.password);
      setSession(response.data);
      setCurrentTab(response.data.role === 'Owner' ? 'stall-editor' : 'poi-list');
      setLoginForm({ username: '', password: '' });
    } catch (error) {
      const handled = handleApiFailure(error);
      if (!handled) {
        alert(error?.response?.data?.message || 'Login failed.');
      }
    } finally {
      setLoginLoading(false);
    }
  };

  const loadPOIs = async () => {
    setLoading(true);
    try {
      const response = await poiService.getAll();
      setPois(response.data);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Load POIs failed', error);
      }
    } finally {
      setLoading(false);
    }
  };

  const loadAccounts = async () => {
    try {
      const response = await accountService.getAll();
      setAccounts(response.data);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Load accounts failed', error);
      }
    }
  };

  const loadOwnerPoi = async () => {
    setLoading(true);
    try {
      const response = await ownerPoiService.getMine();
      setOwnerPoi(response.data);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Load owner POI failed', error);
        alert('Cannot load stall information.');
      }
    } finally {
      setLoading(false);
    }
  };

  const loadDeviceDashboard = async () => {
    try {
      const response = await deviceService.getDashboard();
      setDeviceDashboard(response.data);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Load device dashboard failed', error);
      }
    }
  };

  const handleSavePOI = async (poiData) => {
    setSaving(true);
    try {
      if (editingPoi) {
        await poiService.update(editingPoi.id, poiData);
      } else {
        await poiService.create(poiData);
      }

      setShowForm(false);
      setEditingPoi(null);
      await loadPOIs();
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Save POI failed', error);
        alert(error?.response?.data || 'Save POI failed.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDeletePOI = async (id) => {
    if (!window.confirm('Delete this POI?')) {
      return;
    }

    try {
      await poiService.delete(id);
      await loadPOIs();
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Delete POI failed', error);
        alert('Delete POI failed.');
      }
    }
  };

  const handleUploadImage = async (file) => {
    const response = await poiService.uploadImage(file);
    return response.data.imageUrl;
  };

  const handleOwnerUploadImage = async (file) => {
    const response = await ownerPoiService.uploadImage(file);
    return response.data.imageUrl;
  };

  const handleOwnerSave = async (poiData) => {
    setSaving(true);
    try {
      await ownerPoiService.updateMine({
        name: poiData.name,
        latitude: poiData.latitude,
        longitude: poiData.longitude,
        detectionRadius: poiData.detectionRadius,
        mainImage: poiData.mainImage,
        images: poiData.images,
        translations: poiData.translations,
      });

      await loadOwnerPoi();
      alert('Stall information updated.');
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Owner save failed', error);
        alert(error?.response?.data || 'Save stall failed.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleCreateAccount = async (payload) => {
    try {
      await accountService.create(payload);
      await Promise.all([loadAccounts(), loadPOIs()]);
      alert('Owner account created.');
      return true;
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Create account failed', error);
        alert(error?.response?.data || 'Create account failed.');
      }

      return false;
    }
  };

  const handleToggleAccount = async (account) => {
    try {
      await accountService.updateStatus(account.id, !account.isActive);
      await Promise.all([loadAccounts(), loadPOIs()]);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Update status failed', error);
        alert('Update account status failed.');
      }
    }
  };

  const handleResetPassword = async (account) => {
    const nextPassword = window.prompt(`Enter new password for ${account.username}:`, account.password);
    if (!nextPassword) {
      return;
    }

    try {
      await accountService.updatePassword(account.id, nextPassword);
      await loadAccounts();
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Reset password failed', error);
        alert('Reset password failed.');
      }
    }
  };

  const handleDeleteAccount = async (account) => {
    if (!window.confirm(`Delete account ${account.username} and its stall data?`)) {
      return;
    }

    try {
      await accountService.delete(account.id);
      await Promise.all([loadAccounts(), loadPOIs()]);
    } catch (error) {
      if (!handleApiFailure(error)) {
        console.error('Delete account failed', error);
        alert('Delete account failed.');
      }
    }
  };

  const renderLogin = () => (
    <div className="auth-shell">
      <form className="auth-card" onSubmit={handleLogin}>
        <div className="auth-card__eyebrow">Vinh Khanh CMS</div>
        <h1>Login required</h1>
        <p className="auth-card__text">Admin sees the CMS dashboard. Stall Owner sees only the editing page for their stall.</p>
        <label className="auth-field">
          <span>Username</span>
          <input
            type="text"
            value={loginForm.username}
            onChange={(event) => setLoginForm({ ...loginForm, username: event.target.value })}
            required
          />
        </label>
        <label className="auth-field">
          <span>Password</span>
          <input
            type="password"
            value={loginForm.password}
            onChange={(event) => setLoginForm({ ...loginForm, password: event.target.value })}
            required
          />
        </label>
        <button type="submit" className="btn-primary auth-card__submit" disabled={loginLoading}>
          {loginLoading ? 'Checking...' : 'Login'}
        </button>
      </form>
    </div>
  );

  const renderAdminContent = () => {
    if (currentTab === 'accounts') {
      return (
        <AccountsPanel
          accounts={accounts}
          onCreate={handleCreateAccount}
          onToggleStatus={handleToggleAccount}
          onResetPassword={handleResetPassword}
          onDelete={handleDeleteAccount}
        />
      );
    }

    if (currentTab === 'devices') {
      return (
        <>
          <header className="header-section">
            <div>
              <h1>Connected Devices</h1>
              <p style={{ color: '#64748b' }}>Track mobile devices currently online and see which stalls are viewed the most.</p>
            </div>
            <button className="btn-secondary" onClick={loadDeviceDashboard}>Refresh</button>
          </header>

          <div className="device-stats-grid" style={{ marginBottom: '24px' }}>
            <div className="card">
              <h3 style={{ marginTop: 0 }}>Online now</h3>
              <p className="device-stat">{deviceDashboard.onlineDevices}</p>
            </div>
            <div className="card">
              <h3 style={{ marginTop: 0 }}>Total devices</h3>
              <p className="device-stat">{deviceDashboard.totalDevices}</p>
            </div>
          </div>

          <div className="card" style={{ marginBottom: '24px' }}>
            <h2 style={{ marginTop: 0 }}>Connected mobile devices</h2>
            <table className="poi-table">
              <thead>
                <tr>
                  <th>Device</th>
                  <th>Platform</th>
                  <th>IP</th>
                  <th>Last seen</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {deviceDashboard.devices.length === 0 ? (
                  <tr>
                    <td colSpan="5" className="poi-table__muted">No device heartbeat received yet.</td>
                  </tr>
                ) : deviceDashboard.devices.map((device) => (
                  <tr key={device.deviceId}>
                    <td>
                      <strong>{device.deviceName || 'Unknown device'}</strong>
                      <div className="poi-table__muted">{device.deviceId}</div>
                    </td>
                    <td>{device.platform} / {device.appVersion || 'N/A'}</td>
                    <td>{device.ipAddress}</td>
                    <td>{formatDashboardDateTime(device.lastSeenAt)}</td>
                    <td>
                      <span className={`status-pill ${device.isOnline ? 'status-pill--active' : 'status-pill--inactive'}`}>
                        {device.isOnline ? 'Online' : 'Offline'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="card">
            <h2 style={{ marginTop: 0 }}>Most viewed stalls</h2>
            <table className="poi-table">
              <thead>
                <tr>
                  <th>Stall</th>
                  <th>Total views</th>
                  <th>Unique devices</th>
                  <th>Last viewed</th>
                </tr>
              </thead>
              <tbody>
                {deviceDashboard.topPois.length === 0 ? (
                  <tr>
                    <td colSpan="4" className="poi-table__muted">No stall view statistics yet.</td>
                  </tr>
                ) : deviceDashboard.topPois.map((poi) => (
                  <tr key={poi.poiId}>
                    <td>{poi.poiName}</td>
                    <td>{poi.totalViews}</td>
                    <td>{poi.uniqueDevices}</td>
                    <td>{formatDashboardDateTime(poi.lastViewedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      );
    }

    return (
      <>
        <header className="header-section">
          <div>
            <h1>POI Management</h1>
            <p style={{ color: '#64748b' }}>Sync content for the offline-first mobile app.</p>
          </div>
          <div>
            <button className="btn-secondary" onClick={loadPOIs} style={{ marginRight: '12px' }}>Refresh</button>
            <button className="btn-primary" onClick={() => { setEditingPoi(null); setShowForm(true); }}>+ New POI</button>
          </div>
        </header>

        {showForm && (
          <div className="card" style={{ marginBottom: '24px' }}>
            <POIForm
              initialData={editingPoi}
              onSave={handleSavePOI}
              onCancel={() => { setShowForm(false); setEditingPoi(null); }}
              onUploadImage={handleUploadImage}
              saving={saving}
              mode="admin"
            />
          </div>
        )}

        <div className="card">
          <POITable
            data={pois}
            loading={loading}
            onEdit={(poi) => { setEditingPoi(poi); setShowForm(true); }}
            onDelete={handleDeletePOI}
          />
        </div>
      </>
    );
  };

  const renderOwnerContent = () => {
    if (currentTab === 'stall-info') {
      return (
        <div className="owner-page">
          <header className="header-section">
            <div>
              <h1>Stall Info</h1>
              <p style={{ color: '#64748b' }}>Preview the current data already stored on the server.</p>
            </div>
          </header>

          {ownerPoi ? (
            <div className="card owner-preview">
              <div className="owner-preview__header">
                <div>
                  <h2>Saved Stall Preview</h2>
                  <p>This is the current data already stored on the server and used for mobile sync.</p>
                </div>
                <span className={`status-pill ${ownerPoi.isActive ? 'status-pill--active' : 'status-pill--inactive'}`}>
                  {ownerPoi.isActive ? 'Visible on app' : 'Hidden on app'}
                </span>
              </div>

              <div className="owner-preview__hero">
                {ownerPoi.mainImage ? (
                  <img src={resolveMediaUrl(ownerPoi.mainImage)} alt={ownerPoi.name} className="owner-preview__image" />
                ) : (
                  <div className="owner-preview__image owner-preview__image--placeholder" />
                )}

                <div className="owner-preview__summary">
                  <h3>{ownerPoi.name}</h3>
                  <p>QR: {ownerPoi.qrCode || 'N/A'}</p>
                  <p>Coordinates: {Number(ownerPoi.latitude).toFixed(4)}, {Number(ownerPoi.longitude).toFixed(4)}</p>
                  <p>Detection radius: {ownerPoi.detectionRadius} m</p>
                </div>
              </div>

              <div className="owner-preview__translations">
                {ownerPoi.translations?.map((translation) => (
                  <div key={translation.languageCode} className="owner-preview__translation-card">
                    <strong>{translation.languageCode.toUpperCase()}</strong>
                    <p>{translation.description || 'No short description yet.'}</p>
                    <p>{translation.detailedDescription || 'No detailed description yet.'}</p>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="card">
              <p>Loading stall information...</p>
            </div>
          )}
        </div>
      );
    }

    return (
      <div className="owner-page">
        <header className="header-section">
          <div>
            <h1>Stall Editor</h1>
            <p style={{ color: '#64748b' }}>Update the stall information that will sync to the app.</p>
          </div>
        </header>

        <div className="card">
          {ownerPoi ? (
            <POIForm
              initialData={ownerPoi}
              onSave={handleOwnerSave}
              onCancel={loadOwnerPoi}
              onUploadImage={handleOwnerUploadImage}
              saving={saving}
              mode="owner"
            />
          ) : (
            <p>Loading stall information...</p>
          )}
        </div>
      </div>
    );
  };

  if (!session) {
    return renderLogin();
  }

  return (
    <div className="admin-wrapper">
      <aside className="sidebar">
        <div className="brand">
          <span className="title-top">Vinh Khanh</span>
          <span className="title-main">{pageTitle}</span>
        </div>

        <nav className="nav-menu">
          {isAdmin && (
            <>
              <div className={`nav-item ${currentTab === 'poi-list' ? 'active' : ''}`} onClick={() => setCurrentTab('poi-list')}>
                POI Management
              </div>
              <div className={`nav-item ${currentTab === 'accounts' ? 'active' : ''}`} onClick={() => setCurrentTab('accounts')}>
                Stall Owner Accounts
              </div>
              <div className={`nav-item ${currentTab === 'devices' ? 'active' : ''}`} onClick={() => setCurrentTab('devices')}>
                Connected Devices
              </div>
            </>
          )}

          {isOwner && (
            <>
              <div className={`nav-item ${currentTab === 'stall-editor' ? 'active' : ''}`} onClick={() => setCurrentTab('stall-editor')}>
                Stall Editor
              </div>
              <div className={`nav-item ${currentTab === 'stall-info' ? 'active' : ''}`} onClick={() => setCurrentTab('stall-info')}>
                Stall Info
              </div>
            </>
          )}
        </nav>

        <button type="button" className="btn-secondary sidebar__logout" onClick={handleLogout}>
          Logout
        </button>
      </aside>

      <main className="main-content">
        {isAdmin ? renderAdminContent() : renderOwnerContent()}
      </main>
    </div>
  );
}

export default App;
