import React from 'react';
import { resolveMediaUrl } from '../api';

const POITable = ({ data, loading, onEdit, onDelete }) => {
  if (loading) {
    return <div style={{ padding: '20px' }}>Đang tải...</div>;
  }

  return (
    <table className="poi-table">
      <thead>
        <tr>
          <th>POI</th>
          <th>Toạ độ</th>
          <th>Ảnh</th>
          <th>Trạng thái</th>
          <th className="poi-table__actions-header">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        {data.map((item) => {
          const previewImage = item.mainImage || item.images?.[0];

          return <tr key={item.id}>
            <td>
              <div className="poi-cell">
                {previewImage ? (
                  <img
                    src={resolveMediaUrl(previewImage)}
                    alt={item.name}
                    className="poi-cell__image"
                  />
                ) : (
                  <div className="poi-cell__image poi-cell__image--placeholder" />
                )}
                <div>
                  <strong>{item.name}</strong>
                  <div className="poi-cell__meta">QR: {item.qrCode || 'N/A'}</div>
                </div>
              </div>
            </td>
            <td className="poi-table__muted">
              {Number(item.latitude).toFixed(4)}, {Number(item.longitude).toFixed(4)}
            </td>
            <td>{item.images?.length ?? 0}</td>
            <td>
              <span className={`status-pill ${item.isActive ? 'status-pill--active' : 'status-pill--inactive'}`}>
                {item.isActive ? 'Active' : 'Inactive'}
              </span>
            </td>
            <td className="poi-table__actions">
              <button type="button" className="action-btn action-btn--edit" onClick={() => onEdit(item)}>
                Sửa
              </button>
              <button type="button" className="action-btn action-btn--delete" onClick={() => onDelete(item.id)}>
                Xoá
              </button>
            </td>
          </tr>;
        })}
      </tbody>
    </table>
  );
};

export default POITable;
