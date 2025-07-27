import React, { createContext, useState, useContext, useEffect, useRef } from 'react'; // Добавили useRef
import { createGame, getGame, makeMove } from '../services/api';

const GameContext = createContext();

export const useGame = () => useContext(GameContext);

export const GameProvider = ({ children }) => {
  const [gameState, setGameState] = useState(null);
  const [etag, setEtag] = useState(null);
  const [localPlayer, setLocalPlayer] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // --- НАЧАЛО: Паттерн useRef для борьбы с устаревшим состоянием ---
  // Создаем "коробки" (refs), чтобы хранить в них самые свежие данные.
  const stateRef = useRef({ gameState, etag, localPlayer });

  // После каждого рендера обновляем содержимое "коробок".
  useEffect(() => {
    stateRef.current = { gameState, etag, localPlayer };
  }); // Отсутствие зависимостей означает, что это происходит ПОСЛЕ КАЖДОГО РЕНДЕРА.
  // --- КОНЕЦ: Паттерн useRef ---

  const updateGameState = (data, newEtag) => {
    setGameState(data);
    setEtag(newEtag);
  };

  const clearErrorAfterDelay = () => {
    setTimeout(() => setError(''), 3000);
  };

  // Функции handleCreateGame и handleJoinGame остаются без изменений...
  const handleCreateGame = async () => {
    setLoading(true);
    setError('');
    try {
      const { gameId } = await createGame();
      const { data, etag: newEtag } = await getGame(gameId);
      updateGameState(data, newEtag);
      setLocalPlayer('X');
    } catch (err) {
      setError('Ошибка: Не удалось создать игру.');
      clearErrorAfterDelay();
    } finally {
      setLoading(false);
    }
  };

  const handleJoinGame = async (gameId) => {
    // ... (код без изменений)
    if (!gameId.trim()) {
      setError('Введите ID игры.');
      clearErrorAfterDelay();
      return;
    }
    setLoading(true);
    setError('');
    try {
      const { data, etag: newEtag } = await getGame(gameId);
      updateGameState(data, newEtag);
      setLocalPlayer('O'); // Присоединившийся всегда 'O'
    } catch (err) {
      setError('Ошибка: Игра с таким ID не найдена.');
      clearErrorAfterDelay();
    } finally {
      setLoading(false);
    }
  };


  // --- ИЗМЕНЕНИЕ: handleMakeMove теперь читает данные из refs ---
  // Теперь эта функция гарантированно получит самые свежие данные.
  const handleMakeMove = async (row, column) => {
    // Получаем самое свежее состояние напрямую из "коробки" ref.
    const { gameState: currentGameState, etag: currentEtag, localPlayer: currentLocalPlayer } = stateRef.current;

    // Проверяем условия с самыми свежими данными.
    if (!currentGameState || !currentEtag || currentGameState.status !== 'InProgress' || currentGameState.currentTurn !== currentLocalPlayer) {
      console.error("Выход из handleMakeMove из-за проваленной проверки!", { currentGameState, currentEtag, currentLocalPlayer });
      return;
    }

    setLoading(true);
    setError('');
    try {
      const moveData = { player: currentLocalPlayer, row, column };
      const { data, etag: newEtag } = await makeMove(currentGameState.id, moveData, currentEtag);
      updateGameState(data, newEtag);
    } catch (err) {
      const status = err.response?.status;
      const detail = err.response?.data?.detail;

      if (status === 409) {
        setError('Другой игрок уже сходил! Обновляем доску...');
        const { data, etag: newEtag } = await getGame(currentGameState.id);
        updateGameState(data, newEtag);
      } else {
        setError(`Ошибка: ${detail || 'Не удалось сделать ход.'}`);
      }
      clearErrorAfterDelay();
    } finally {
      setLoading(false);
    }
  };

  // Поллинг остается без изменений
  useEffect(() => {
    if (!gameState || gameState.status !== 'InProgress') {
      return;
    }
    const intervalId = setInterval(async () => {
      // Здесь мы можем использовать etag из state, т.к. этот эффект сам зависит от него
      if (stateRef.current.etag === etag) {
          const { data, etag: newEtag } = await getGame(gameState.id);
          if (newEtag !== etag) {
            updateGameState(data, newEtag);
          }
      }
    }, 3000);
    return () => clearInterval(intervalId);
  }, [gameState, etag]); // Зависимости верные


  const value = {
    gameState,
    localPlayer,
    loading,
    error,
    handleCreateGame,
    handleJoinGame,
    handleMakeMove,
  };

  return <GameContext.Provider value={value}>{children}</GameContext.Provider>;
};