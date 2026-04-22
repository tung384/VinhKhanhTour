import React, { useEffect, useState } from 'react';
import { resolveMediaUrl } from '../api';

const defaultTranslations = [
  { languageCode: 'vi', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'en', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'ko', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
  { languageCode: 'fr', description: '', detailedDescription: '', audioScript: '', audioUrl: '' },
];

const MAX_IMAGE_COUNT = 10;

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

    const payload = {
      ...formData,
      latitude: parseFloat(formData.latitude),
      longitude: parseFloat(formData.longitude),
      detectionRadius: parseFloat(formData.detectionRadius),
      priority: parseInt(formData.priority, 10),
      images: formData.images.map((imageUrl) => ({ imageUrl })),
      translations: formData.translations.filter((translation) =>
        translation.languageCode === 'vi' ||
        translation.description ||
        translation.detailedDescription ||
        translation.audioScript ||
        translation.audioUrl
      ),
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
      <h3 style={{ marginTop: 0 }}>{isOwnerMode ? 'Cap nhat thong tin stall' : initialData ? 'Chinh sua POI' : 'Them POI moi'}</h3>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: '16px' }}>
        <label>
          <div>Ten dia diem</div>
          <input type="text" value={formData.name} onChange={(event) => setFormData({ ...formData, name: event.target.value })} required />
        </label>

        {!isOwnerMode && (
          <label>
            <div>Ma QR</div>
            <input type="text" value={formData.qrCode} onChange={(event) => setFormData({ ...formData, qrCode: event.target.value })} required />
          </label>
        )}

        <label>
          <div>Vi do</div>
          <input type="number" step="any" value={formData.latitude} onChange={(event) => setFormData({ ...formData, latitude: event.target.value })} required />
        </label>

        <label>
          <div>Kinh do</div>
          <input type="number" step="any" value={formData.longitude} onChange={(event) => setFormData({ ...formData, longitude: event.target.value })} required />
        </label>

        <label>
          <div>Ban kinh phat hien (m)</div>
          <input type="number" step="any" value={formData.detectionRadius} onChange={(event) => setFormData({ ...formData, detectionRadius: event.target.value })} required />
        </label>

        {!isOwnerMode && (
          <label>
            <div>Uu tien</div>
            <input type="number" min="1" value={formData.priority} onChange={(event) => setFormData({ ...formData, priority: event.target.value })} required />
          </label>
        )}
      </div>

      {!isOwnerMode && (
        <div style={{ marginTop: '16px' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <input type="checkbox" checked={formData.isActive} onChange={(event) => setFormData({ ...formData, isActive: event.target.checked })} />
            Kich hoat hien thi tren app
          </label>
        </div>
      )}

      <div style={{ marginTop: '20px' }}>
        <h4>Hinh anh</h4>
        <p className="form-hint">Tat ca anh duoc upload tai day se hien o trang chi tiet stall. Anh duoc chon la anh chinh se hien o danh sach stall va van nam trong bo anh chi tiet.</p>
        <p className="form-hint">Toi da {MAX_IMAGE_COUNT} anh cho moi stall. MySQL chi luu URL, file goc duoc luu tren server local.</p>
        <input type="file" accept="image/*" multiple onChange={handleFilesSelected} disabled={uploading || saving || formData.images.length >= MAX_IMAGE_COUNT} />
        {uploading && <p>Dang tai anh len server...</p>}
        <p className="form-hint">Da chon {formData.images.length}/{MAX_IMAGE_COUNT} anh.</p>

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
                  Anh chinh
                </label>
                <button type="button" className="image-card__remove" onClick={() => removeImage(imageUrl)}>
                  Xoa anh
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div style={{ marginTop: '20px' }}>
        <h4>Noi dung da ngon ngu</h4>
        {formData.translations.map((translation) => (
          <div key={translation.languageCode} style={{ border: '1px solid #e2e8f0', borderRadius: '8px', padding: '12px', marginBottom: '12px' }}>
            <strong>{translation.languageCode.toUpperCase()}</strong>
            <div style={{ display: 'grid', gap: '10px', marginTop: '10px' }}>
              <input
                type="text"
                placeholder="Mo ta ngan"
                value={translation.description}
                onChange={(event) => updateTranslation(translation.languageCode, 'description', event.target.value)}
              />
              <textarea
                placeholder="Mo ta chi tiet"
                rows="3"
                value={translation.detailedDescription}
                onChange={(event) => updateTranslation(translation.languageCode, 'detailedDescription', event.target.value)}
              />
              <textarea
                placeholder="Kich ban TTS"
                rows="3"
                value={translation.audioScript}
                onChange={(event) => updateTranslation(translation.languageCode, 'audioScript', event.target.value)}
              />
            </div>
          </div>
        ))}
      </div>

      <div style={{ marginTop: '20px', display: 'flex', gap: '10px' }}>
        <button type="submit" className="btn-primary" disabled={saving || uploading}>
          {saving ? 'Dang luu...' : 'Luu thay doi'}
        </button>
        <button type="button" className="btn-secondary" onClick={onCancel} disabled={saving || uploading}>
          Huy
        </button>
      </div>
    </form>
  );
};

export default POIForm;
