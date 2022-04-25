from datetime import datetime, timedelta
from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient


@pytest.mark.parametrize(
    "query_params, expected_len",
    [
        ("?robot_id=1", 1),
        ("", 2),
        (f"?min_start_time={datetime.max}", 0),
        (f"?max_start_time={datetime.utcnow()-timedelta(hours=0.6)}", 1),
    ],
)
def test_get_reports(test_app: FastAPI, query_params: str, expected_len: int):
    with TestClient(test_app) as client:
        response = client.get("/reports" + query_params)
        assert response.status_code == HTTPStatus.OK.value
        assert len(response.json()) == expected_len


@pytest.mark.parametrize(
    "report_id, expected_status_code",
    [
        (1, HTTPStatus.OK.value),
        (3, HTTPStatus.NOT_FOUND.value),
    ],
)
def test_get_report(test_app: FastAPI, report_id: int, expected_status_code: int):
    with TestClient(test_app) as client:
        response = client.get(f"/reports/{report_id}")
        assert response.status_code == expected_status_code
