// src/components/GameBoard.js

import React from 'react';
import { useGame } from '../context/GameContext';
import Cell from './Cell';

function GameBoard() {
  const { gameState } = useGame();

  if (!gameState) return null;

  // Здесь мы используем gameState.boardSize, которое приходит в camelCase
  const boardStyle = {
    display: 'grid', // Убедимся что это grid
    gridTemplateColumns: `repeat(${gameState.boardSize}, 1fr)`,
    gap: '5px',
    maxWidth: '450px',
    margin: '20px auto',
  };

  // gameState.board тоже приходит в camelCase
  return (
    <div style={boardStyle}>
      {gameState.board.flat().map((cellValue, index) => {
        const rowIndex = Math.floor(index / gameState.boardSize);
        const colIndex = index % gameState.boardSize;
        return (
          <Cell
            key={index}
            value={cellValue}
            rowIndex={rowIndex}
            colIndex={colIndex}
          />
        );
      })}
    </div>
  );
}

export default GameBoard;