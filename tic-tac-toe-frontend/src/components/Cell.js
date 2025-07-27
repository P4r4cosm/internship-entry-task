import React from 'react';
import { useGame } from '../context/GameContext';

function Cell({ value, rowIndex, colIndex }) {
  const { gameState, localPlayer, loading, handleMakeMove } = useGame();

  const isMyTurn = gameState.status === 'InProgress' && gameState.currentTurn === localPlayer;
  const canClick = !value && isMyTurn && !loading;

  const handleClick = () => {
    // --- НАЧАЛО ДИАГНОСТИЧЕСКОГО БЛОКА ---
    // Этот код выведет в консоль все переменные, от которых зависит клик.
    console.log('--- Cell Click Diagnostics ---', {
      // Итоговое решение:
      canClick, // Должно быть true для клика

      // ----------------------------------
      // Проверка условий для `canClick`:
      isCellEmpty: !value, // Должно быть true
      isMyTurn, // Должно быть true
      isNotLoading: !loading, // Должно быть true
      
      // ----------------------------------
      // Проверка ДАННЫХ для `isMyTurn` (самое важное):
      'gameState.status': gameState.status, // Ожидаем: "InProgress"
      'gameState.currentTurn': gameState.currentTurn, // Ожидаем: "X"
      'localPlayer': localPlayer, // Ожидаем: "X"
      
      // ----------------------------------
      // Прямое сравнение, которое определяет `isMyTurn`
      'is status InProgress?': gameState.status === 'InProgress',
      'is turn matching?': gameState.currentTurn === localPlayer
    });
    // --- КОНЕЦ ДИАГНОСТИЧЕСКОГО БЛОКА ---

    if (canClick) {
      handleMakeMove(rowIndex, colIndex);
    }
  };

  return (
    <button className="cell" onClick={handleClick} disabled={!canClick}>
      {value}
    </button>
  );
}

export default Cell;