import React from 'react';
import { useGame } from '../context/GameContext';

function GameInfo() {
  const { gameState, localPlayer } = useGame();

  // ИСПРАВЛЕННАЯ ФУНКЦИЯ
  const getStatusMessage = () => {
    if (!gameState) return '';
    // Мы меняем все 'PascalCase' на 'camelCase'
    switch (gameState.status) {
      case 'InProgress':
        return `Ход игрока: ${gameState.currentTurn}`;
      case 'XWins': // xWins вместо XWins
        return '🎉 Победил X! 🎉';
      case 'OWins': // oWins вместо OWins
        return '🎉 Победил O! 🎉';
      case 'Draw': // draw вместо Draw
        return 'Ничья!';
      default:
        return 'Статус неизвестен';
    }
  };


  return (
    <div className="game-info">
      <p>
        <strong>ID Игры:</strong> {gameState.id}
        <button onClick={() => navigator.clipboard.writeText(gameState.id)} className="copy-btn">
          📋
        </button>
      </p>
      <p><strong>Вы играете за:</strong> {localPlayer}</p>
      <h2 className="status">{getStatusMessage()}</h2>
    </div>
  );
}

export default GameInfo;