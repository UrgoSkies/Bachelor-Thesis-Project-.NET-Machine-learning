document.getElementById('ratingForm').addEventListener('submit', function (event) {
    event.preventDefault();
    const userId = document.getElementById('user').value;
    const ratings = [
        { UserId: userId, ISBN: 1, BookRating: document.getElementById('book1').value },
        { UserId: userId, ISBN: 2, BookRating: document.getElementById('book2').value },
        { UserId: userId, ISBN: 3, BookRating: document.getElementById('book3').value },
        { UserId: userId, ISBN: 4, BookRating: document.getElementById('book4').value },
        { UserId: userId, ISBN: 5, BookRating: document.getElementById('book5').value }
    ];

    fetch('/api/recommendations/rate', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(ratings)
    })
        .then(response => response.json())
        .then(data => {
            console.log('Ratings submitted:', data);
            fetchRecommendations(userId);
        })
        .catch((error) => {
            console.error('Error:', error);
        });
});

function fetchRecommendations(userId) {
    fetch('/api/recommendations/recommend', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ userId })
    })
        .then(response => response.json())
        .then(data => {
            const list = document.getElementById('recommendationsList');
            list.innerHTML = '';
            const labels = [];
            const ratings = [];
            data.forEach(item => {
                const li = document.createElement('li');
                li.textContent = `Книга: ${item.bookTitle}, Предсказанный рейтинг: ${item.predictedRating.toFixed(2)}`;
                list.appendChild(li);
                labels.push(item.bookTitle);
                ratings.push(item.predictedRating);
            });

            const ctx = document.getElementById('recommendationChart').getContext('2d');
            new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Предсказанные рейтинги',
                        data: ratings,
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        borderColor: 'rgba(75, 192, 192, 1)',
                        borderWidth: 1
                    }]
                },
                options: {
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
        });
}
