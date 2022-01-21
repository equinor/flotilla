import pytest
from fastapi.testclient import TestClient

from flotilla.main import app


def test_read_robots():
    with TestClient(app) as client:
        response = client.get("/robots")
        assert response.status_code == 200
        assert len(response.json()) == 2


@pytest.mark.parametrize(
    "robot_id, expected_status_code",
    [
        (1, 200),
        (23, 404),
    ],
)
def test_read_robot(robot_id, expected_status_code):
    with TestClient(app) as client:
        response = client.get(f"/robots/{robot_id}")
        assert response.status_code == expected_status_code
