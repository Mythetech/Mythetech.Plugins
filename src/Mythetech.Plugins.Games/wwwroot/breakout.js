window.breakoutGame = (function () {
    var canvasElement = null;

    function setupCanvas(canvasRef) {
        canvasElement = canvasRef;
    }

    function getCanvasX(canvasRef, clientX) {
        var canvas = canvasRef;
        if (!canvas) return 0;
        var rect = canvas.getBoundingClientRect();
        return clientX - rect.left;
    }

    function drawBreakout(params) {
        var canvas = params.canvasRef;
        if (!canvas) return;

        var ctx = canvas.getContext('2d');
        if (!ctx) return;

        ctx.clearRect(0, 0, 800, 600);

        ctx.fillStyle = '#fff';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Score: ' + params.score + ' | Bricks: ' + params.bricksRemaining, 400, 30);

        var paddleX = params.paddleX;
        var paddleY = 580;
        var paddleWidth = 100;
        var paddleHeight = 10;

        ctx.fillStyle = '#4CAF50';
        ctx.fillRect(paddleX, paddleY, paddleWidth, paddleHeight);

        var ballX = params.ballX;
        var ballY = params.ballY;
        var ballSize = 10;

        ctx.fillStyle = '#fff';
        ctx.beginPath();
        ctx.arc(ballX + ballSize / 2, ballY + ballSize / 2, ballSize / 2, 0, Math.PI * 2);
        ctx.fill();

        var brickRows = 5;
        var brickCols = 10;
        var brickWidth = 70;
        var brickHeight = 20;
        var brickPadding = 5;
        var brickOffsetTop = 50;
        var brickOffsetLeft = 35;

        var colors = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#FFA07A', '#98D8C8'];
        var bricks = params.bricks || [];

        for (var row = 0; row < brickRows; row++) {
            for (var col = 0; col < brickCols; col++) {
                var index = row * brickCols + col;
                if (bricks[index] === 1) {
                    var brickX = col * (brickWidth + brickPadding) + brickOffsetLeft;
                    var brickY = row * (brickHeight + brickPadding) + brickOffsetTop;

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
            var message = params.bricksRemaining === 0 ? 'You Win!' : 'Game Over!';
            ctx.fillText(message, 400, 280);

            ctx.font = '20px Arial';
            ctx.fillText('Click to play again', 400, 320);
        }
    }

    return {
        setupCanvas: setupCanvas,
        getCanvasX: getCanvasX,
        drawBreakout: drawBreakout
    };
})();
