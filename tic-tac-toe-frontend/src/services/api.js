import axios from 'axios';
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5071/api';

// Настройте базовый URL вашего API
const apiClient = axios.create({
  baseURL: API_BASE_URL, // Убедитесь, что порт верный
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Создает новую игру.
 * @returns {Promise<{gameId: string}>}
 */
export const createGame = async () => {
  const response = await apiClient.post('/games', {});
  // ИЗМЕНЕНИЕ ЗДЕСЬ:
  // Мы больше не читаем заголовок Location.
  // Мы напрямую возвращаем тело ответа, которое содержит gameId.
  // response.data — это { gameId: "..." }
  return response.data;
};
/**
 * Получает состояние игры по ID.
 * @param {string} id - ID игры
 * @returns {Promise<{data: object, etag: string}>} - Возвращает данные игры и ETag
 */
export const getGame = async (id) => {
  const response = await apiClient.get(`/games/${id}`);
  return {
    data: response.data,
    etag: response.headers.etag, // ETag критически важен
  };
};

/**
 * Выполняет ход в игре.
 * @param {string} id - ID игры
 * @param {{player: string, row: number, column: number}} moveData - Данные о ходе
 * @param {string} etag - Текущий ETag для контроля параллельного доступа
 * @returns {Promise<{data: object, etag: string}>} - Возвращает обновленное состояние и новый ETag
 */
export const makeMove = async (id, moveData, etag) => {
  const response = await apiClient.post(`/games/${id}/moves`, moveData, {
    headers: {
      'If-Match': etag, // Отправляем ETag для проверки на сервере
    },
  });
  return {
    data: response.data,
    etag: response.headers.etag,
  };
};