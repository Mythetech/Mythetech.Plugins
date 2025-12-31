let canvasElement = null;

export function setupCanvas(canvasRef) {
    canvasElement = canvasRef;
}

export function getCanvasX(canvasRef, clientX) {
    const canvas = canvasRef;
    if (!canvas) return 0;
    const rect = canvas.getBoundingClientRect();
    return clientX - rect.left;
}

export function drawBreakout(params) {
    const canvas = params.canvasRef;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.clearRect(0, 0, 800, 600);

    ctx.fillStyle = '#fff';
    ctx.font = '16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText(`Score: ${params.score} | Bricks: ${params.bricksRemaining}`, 400, 30);

    const paddleX = params.paddleX;
    const paddleY = 580;
    const paddleWidth = 100;
    const paddleHeight = 10;

    ctx.fillStyle = '#4CAF50';
    ctx.fillRect(paddleX, paddleY, paddleWidth, paddleHeight);

    const ballX = params.ballX;
    const ballY = params.ballY;
    const ballSize = 10;

    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.arc(ballX + ballSize / 2, ballY + ballSize / 2, ballSize / 2, 0, Math.PI * 2);
    ctx.fill();

    const brickRows = 5;
    const brickCols = 10;
    const brickWidth = 70;
    const brickHeight = 20;
    const brickPadding = 5;
    const brickOffsetTop = 50;
    const brickOffsetLeft = 35;

    const colors = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#FFA07A', '#98D8C8'];
    const bricks = params.bricks || [];

    for (let row = 0; row < brickRows; row++) {
        for (let col = 0; col < brickCols; col++) {
            const index = row * brickCols + col;
            if (bricks[index] === 1) {
                const brickX = col * (brickWidth + brickPadding) + brickOffsetLeft;
                const brickY = row * (brickHeight + brickPadding) + brickOffsetTop;
                
                ctx.fillStyle = colors[row % colors.length];
                ctx.fillRect(brickX, brickY, brickWidth, brickHeight);
                
                ctx.strokeStyle = '#fff';
                ctx.lineWidth = 1;
                ctx.strokeRect(brickX, brickY, brickWidth, brickHeight);
            }
        }
    }

    if (params.gameOver) {
        ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
        ctx.fillRect(0, 0, 800, 600);
        
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 32px Arial';
        ctx.textAlign = 'center';
        const message = params.bricksRemaining === 0 ? 'You Win!' : 'Game Over!';
        ctx.fillText(message, 400, 280);
        
        ctx.font = '20px Arial';
        ctx.fillText('Click to play again', 400, 320);
    }
}

