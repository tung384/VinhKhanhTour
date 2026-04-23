import React, { useEffect, useState } from 'react';
import { resolveMediaUrl } from '../api';

const defaultTranslations = [
  { languageCode: 'vi', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'en', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'ko', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'fr', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
];

const MAX_IMAGE_COUNT = 10;

const readOnlySectionStyle = {
  marginTop: '20px',
  opacity: 0.6,
  pointerEvents: 'none',
};

const createInitialFormData = (initialData) => {
  if (!initialData) {
    return {
      name: '',
      latitude: '',
      longitude: '',
      detectionRadius: 25,
      priority: 1,
      qrCode: '',
      isActive: true,
      mainImage: '',
      images: [],
      translations: defaultTranslations,
    };
  }

  const mergedTranslations = defaultTranslations.map((translation) => {
    const existing = initialData.translations?.find((item) => item.languageCode === translation.languageCode);
    return {
      ...translation,
      ...existing,
      audioScript: existing?.audioScript ?? existing?.detailedDescription ?? '',
      audioUrl: existing?.audioUrl ?? '',
    };
  });

  return {
    name: initialData.name ?? '',
    latitude: initialData.latitude ?? '',
    longitude: initialData.longitude ?? '',
    detectionRadius: initialData.detectionRadius ?? 25,
    priority: initialData.priority ?? 1,
    qrCode: initialData.qrCode ?? '',
    isActive: initialData.isActive ?? true,
    mainImage: initialData.mainImage ?? initialData.images?.[0] ?? '',
    images: initialData.images ?? [],
    translations: mergedTranslations,
  };
};

const POIForm = ({ initialData, onSave, onCancel, onUploadImage, saving, mode = 'admin' }) => {
  const [formData, setFormData] = useState(createInitialFormData(initialData));
  const [uploading, setUploading] = useState(false);
  const isOwnerMode = mode === 'owner';

  useEffect(() => {
    setFormData(createInitialFormData(initialData));
  }, [initialData]);

  const updateTranslation = (languageCode, field, value) => {
    setFormData((current) => ({
      ...current,
      translations: current.translations.map((translation) =>
        translation.languageCode === languageCode
          ? { ...translation, [field]: value }
          : translation
      )
    }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();

    const filteredTranslations = formData.translations.filter((translation) =>
      translation.description ||
      translation.detailedDescription ||
      translation.audioScript ||
      translation.audioUrl ||
      (isOwnerMode && translation.languageCode === 'vi')
    );

    const payload = {
      ...formData,
      latitude: parseFloat(formData.latitude),
      longitude: parseFloat(formData.longitude),
      detectionRadius: parseFloat(formData.detectionRadius),
      priority: parseInt(formData.priority, 10),
      images: formData.images.map((imageUrl) => ({ imageUrl })),
      translations: filteredTranslations,
    };

    onSave(payload);
  };

  const handleFilesSelected = async (event) => {
    const files = Array.from(event.target.files ?? []);
    if (files.length === 0) {
      return;
    }

    if (formData.images.length >= MAX_IMAGE_COUNT) {
      event.target.value = '';
      return;
    }

    const remainingSlots = MAX_IMAGE_COUNT - formData.images.length;
    const filesToUpload = files.slice(0, remainingSlots);

    setUploading(true);

    try {
      const uploadedUrls = [];

      for (const file of filesToUpload) {
        const imageUrl = await onUploadImage(file);
        uploadedUrls.push(imageUrl);
      }

      setFormData((current) => {
        const nextImages = [...current.images, ...uploadedUrls];
        return {
          ...current,
          images: nextImages,
          mainImage: current.mainImage || nextImages[0] || '',
        };
      });
    } finally {
      setUploading(false);
      event.target.value = '';
    }
  };

  const removeImage = (imageUrl) => {
    setFormData((current) => {
      const nextImages = current.images.filter((item) => item !== imageUrl);
      return {
        ...current,
        images: nextImages,
        mainImage: current.mainImage === imageUrl ? (nextImages[0] ?? '') : current.mainImage,
      };
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      <h3 style={{ marginTop: 0 }}>{isOwnerMode ? 'Cập nhật thông tin stall' : initialData ? 'Chỉnh sửa POI' : 'Thêm POI mới'}</h3>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: '16px' }}>
        <label>
          <div>Tên địa điểm</div>
          <input type="text" value={formData.name} onChange={(event) => setFormData({ ...formData, name: event.target.value })} required />
        </label>

        {!isOwnerMode && (
          <label>
            <div>Mã QR</div>
            <input type="text" value={formData.qrCode} onChange={(event) => setFormData({ ...formData, qrCode: event.target.value })} required />
          </label>
        )}

        <label>
          <div>Vĩ độ</div>
          <input type="number" step="any" value={formData.latitude} onChange={(event) => setFormData({ ...formData, latitude: event.target.value })} required disabled={isOwnerMode} />
        </label>

        <label>
          <div>Kinh độ</div>
          <input type="number" step="any" value={formData.longitude} onChange={(event) => setFormData({ ...formData, longitude: event.target.value })} required disabled={isOwnerMode} />
        </label>

        <label>
          <div>Bán kính phát hiện (m)</div>
          <input type="number" step="any" value={formData.detectionRadius} onChange={(event) => setFormData({ ...formData, detectionRadius: event.target.value })} required disabled={isOwnerMode} />
        </label>

        {!isOwnerMode && (
          <label>
            <div>Ưu tiên</div>
            <input type="number" min="1" value={formData.priority} onChange={(event) => setFormData({ ...formData, priority: event.target.value })} required />
          </label>
        )}
      </div>

      {!isOwnerMode && (
        <div style={{ marginTop: '16px' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <input type="checkbox" checked={formData.isActive} onChange={(event) => setFormData({ ...formData, isActive: event.target.checked })} />
            Kích hoạt hiển thị trên app
          </label>
        </div>
      )}

      {isOwnerMode ? (
        <>
          <div style={{ marginTop: '20px' }}>
            <h4>Hinh anh</h4>
            <p className="form-hint">Các ảnh được tải lên sẽ hiển thị trong trang chi tiết quán. Vui lòng chọn 1 trong số đó làm ảnh đại diện quán</p>
            <input type="file" accept="image/*" multiple onChange={handleFilesSelected} disabled={uploading || saving || formData.images.length >= MAX_IMAGE_COUNT} />
            {uploading && <p>Đang tải ảnh lên server...</p>}
            <p className="form-hint">Đã chọn {formData.images.length}/{MAX_IMAGE_COUNT} ảnh.</p>

            {formData.images.length > 0 && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '12px', marginTop: '16px' }}>
                {formData.images.map((imageUrl) => (
                  <div key={imageUrl} className="image-card">
                    <img src={resolveMediaUrl(imageUrl)} alt="POI" className="image-card__preview" />
                    <label style={{ display: 'block', marginTop: '8px', fontSize: '0.85rem' }}>
                      <input
                        type="radio"
                        name="mainImage"
                        checked={formData.mainImage === imageUrl}
                        onChange={() => setFormData({ ...formData, mainImage: imageUrl })}
                      />
                      Ảnh chính
                    </label>
                    <button type="button" className="image-card__remove" onClick={() => removeImage(imageUrl)}>
                      Xóa ảnh
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div style={{ marginTop: '20px' }}>
            <h4>Nội dung đa ngôn ngữ</h4>
            {formData.translations.map((translation) => (
              <div key={translation.languageCode} style={{ border: '1px solid #e2e8f0', borderRadius: '8px', padding: '12px', marginBottom: '12px' }}>
                <strong>{translation.languageCode.toUpperCase()}</strong>
                <div style={{ display: 'grid', gap: '10px', marginTop: '10px' }}>
                  <input
                    type="text"
                    placeholder="Mô tả ngắn"
                    value={translation.description}
                    onChange={(event) => updateTranslation(translation.languageCode, 'description', event.target.value)}
                  />
                  <textarea
                    placeholder="Mô tả chi tiết"
                    rows="3"
                    value={translation.detailedDescription}
                    onChange={(event) => updateTranslation(translation.languageCode, 'detailedDescription', event.target.value)}
                  />
                  <textarea
                    placeholder="Kịch bản TTS"
                    rows="3"
                    value={translation.audioScript}
                    onChange={(event) => updateTranslation(translation.languageCode, 'audioScript', event.target.value)}
                  />
                </div>
              </div>
            ))}
          </div>
        </>
      ) : (
        <>
          <div style={readOnlySectionStyle}>
            <h4>Hình ảnh</h4>
            <p className="form-hint">Phần này do Stall Owner quản lý. Admin chỉ xem</p>
            <p className="form-hint">Đã có {formData.images.length}/{MAX_IMAGE_COUNT} ảnh.</p>

            {formData.images.length > 0 ? (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '12px', marginTop: '16px' }}>
                {formData.images.map((imageUrl) => (
                  <div key={imageUrl} className="image-card image-card--readonly">
                    <img src={resolveMediaUrl(imageUrl)} alt="POI" className="image-card__preview" />
                    <label style={{ display: 'block', marginTop: '8px', fontSize: '0.85rem' }}>
                      <input
                        type="radio"
                        name="mainImageReadonly"
                        checked={formData.mainImage === imageUrl}
                        readOnly
                        disabled
                      />
                      Anh chinh
                    </label>
                    <button type="button" className="image-card__remove" disabled>
                      Xoa anh
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <div className="readonly-empty">Chua co anh duoc stall owner cap nhat.</div>
            )}
          </div>

          <div style={readOnlySectionStyle}>
            <h4>Nội dung đa ngôn ngữ</h4>
            <p className="form-hint">Phần này do Stall Owner quản lý. Admin chỉ xem</p>
            {formData.translations.map((translation) => (
              <div key={translation.languageCode} className="readonly-translation-card">
                <strong>{translation.languageCode.toUpperCase()}</strong>
                <div style={{ display: 'grid', gap: '10px', marginTop: '10px' }}>
                  <input type="text" value={translation.description} readOnly disabled />
                  <textarea rows="3" value={translation.detailedDescription} readOnly disabled />
                  <textarea rows="3" value={translation.audioScript} readOnly disabled />
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      <div style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
        <button type="submit" className="btn-primary" disabled={saving || uploading}>
          {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
        </button>
        <button type="button" className="btn-secondary" onClick={onCancel} disabled={saving || uploading}>
          Huỷ
        </button>
      </div>
    </form>
  );
};

export default POIForm;
