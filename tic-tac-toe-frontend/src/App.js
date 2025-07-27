import React from 'react';
import { GameProvider, useGame } from './context/GameContext';
import GameBoard from './components/GameBoard';
import GameInfo from './components/GameInfo';
import GameSetup from './components/GameSetup';
import './App.css'; // Импортируем стили

// Внутренний компонент для доступа к контексту
const GameView = () => {
  const { gameState, error, loading } = useGame();

  return (
    <div className="app-container">
      <header>
        <h1>Крестики-Нолики .NET</h1>
      </header>
      <main>
        {loading && <div className="spinner"></div>}
        {error && <div className="error-popup">{error}</div>}
        {!gameState ? <GameSetup /> : 
          <>
            <GameInfo />
            <GameBoard />
          </>
        }
      </main>
    </div>
  );
};

// Основной компонент App
function App() {
  return (
    <GameProvider>
      <GameView />
    </GameProvider>
  );
}

export default App;