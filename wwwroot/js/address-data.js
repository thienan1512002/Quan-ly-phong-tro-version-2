/**
 * Địa chỉ Việt Nam — 2 cấp (Tỉnh/TP → Phường/Xã)
 * Nguồn: https://provinces.open-api.vn/api/v2/
 */
const ADDRESS_API = "https://provinces.open-api.vn/api/v2";

// Cache tránh gọi API nhiều lần
let _provinceList = null;
const _wardCache = {}; // provinceCode → [{code, name}]

/**
 * Lấy toàn bộ danh sách tỉnh/thành phố
 * @returns {Promise<{code:number, name:string}[]>}
 */
async function fetchProvinces() {
    if (_provinceList) return _provinceList;
    const res = await fetch(`${ADDRESS_API}/p/`);
    const data = await res.json();
    _provinceList = data.map((p) => ({ code: p.code, name: p.name }));
    return _provinceList;
}

/**
 * Lấy danh sách phường/xã theo mã tỉnh
 * @param {number} provinceCode
 * @returns {Promise<{code:number, name:string}[]>}
 */
async function fetchWards(provinceCode) {
    if (!provinceCode) return [];
    if (_wardCache[provinceCode]) return _wardCache[provinceCode];
    const res = await fetch(`${ADDRESS_API}/w/?province=${provinceCode}`);
    const data = await res.json();
    const list = (data || []).map((w) => ({ code: w.code, name: w.name }));
    _wardCache[provinceCode] = list;
    return list;
}