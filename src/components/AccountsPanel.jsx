import React, { useState } from 'react';

const initialForm = {
  username: '',
  password: '',
  stallName: '',
  qrCode: '',
  isActive: true,
};

const AccountsPanel = ({ accounts, onCreate, onToggleStatus, onResetPassword, onDelete }) => {
  const [form, setForm] = useState(initialForm);
  const ownerAccounts = accounts.filter((account) => account.role === 'Owner');

  const handleSubmit = async (event) => {
    event.preventDefault();
    const created = await onCreate(form);
    if (created) {
      setForm(initialForm);
    }
  };

  return (
    <>
      <div className="card" style={{ marginBottom: '24px' }}>
        <header className="header-section" style={{ marginBottom: '20px' }}>
          <div>
            <h1>Stall Owner Accounts</h1>
            <p style={{ color: '#64748b' }}>Creating an owner account also creates a new POI for that stall.</p>
          </div>
        </header>

        <form className="account-form" onSubmit={handleSubmit}>
          <label>
            <span>Username</span>
            <input
              type="text"
              value={form.username}
              onChange={(event) => setForm({ ...form, username: event.target.value })}
              required
            />
          </label>
          <label>
            <span>Password</span>
            <input
              type="text"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
              required
            />
          </label>
          <label>
            <span>Stall Name</span>
            <input
              type="text"
              value={form.stallName}
              onChange={(event) => setForm({ ...form, stallName: event.target.value })}
              required
            />
          </label>
          <label>
            <span>QR Code</span>
            <input
              type="text"
              value={form.qrCode}
              onChange={(event) => setForm({ ...form, qrCode: event.target.value })}
              required
            />
          </label>
          <label className="account-form__checkbox">
            <span>Visible on app</span>
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
            />
          </label>
          <button type="submit" className="btn-primary">Create owner account</button>
        </form>
      </div>

      <div className="card">
        <table className="poi-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Password</th>
              <th>Stall</th>
              <th>Status</th>
              <th className="poi-table__actions-header">Actions</th>
            </tr>
          </thead>
          <tbody>
            {ownerAccounts.map((account) => (
              <tr key={account.id}>
                <td>{account.username}</td>
                <td><code>{account.password}</code></td>
                <td>{account.poiName || `POI #${account.poiId}`}</td>
                <td>
                  <button
                    type="button"
                    className={`switch-btn ${account.isActive ? 'switch-btn--on' : 'switch-btn--off'}`}
                    onClick={() => onToggleStatus(account)}
                  >
                    <span className="switch-btn__thumb" />
                    <span>{account.isActive ? 'On' : 'Off'}</span>
                  </button>
                </td>
                <td className="poi-table__actions">
                  <button type="button" className="action-btn action-btn--edit" onClick={() => onResetPassword(account)}>
                    Reset password
                  </button>
                  <button type="button" className="action-btn action-btn--delete" onClick={() => onDelete(account)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
};

export default AccountsPanel;
