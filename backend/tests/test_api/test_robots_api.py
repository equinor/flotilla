import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient


def test_read_robots(test_app: FastAPI):
    with TestClient(test_app) as client:
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
def test_read_robot(test_app: FastAPI, robot_id: int, expected_status_code: int):
    with TestClient(test_app) as client:
        response = client.get(f"/robots/{robot_id}")
        assert response.status_code == expected_status_code
