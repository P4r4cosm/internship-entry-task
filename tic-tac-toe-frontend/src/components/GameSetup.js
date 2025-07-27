import React, { useState } from 'react';
import { useGame } from '../context/GameContext';

function GameSetup() {
  const { handleCreateGame, handleJoinGame, loading } = useGame();
  const [joinId, setJoinId] = useState('');

  return (
    <div className="game-setup">
      <h2>Начать игру</h2>
      <button onClick={handleCreateGame} disabled={loading} className="btn-create">
        Создать новую игру
      </button>
      <div className="divider">или</div>
      <div className="join-section">
        <input
          type="text"
          placeholder="Введите ID для подключения"
          value={joinId}
          onChange={(e) => setJoinId(e.target.value)}
          disabled={loading}
        />
        <button onClick={() => handleJoinGame(joinId)} disabled={loading}>
          Присоединиться
        </button>
      </div>
    </div>
  );
}

export default GameSetup;