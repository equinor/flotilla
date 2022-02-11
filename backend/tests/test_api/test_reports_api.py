from http import HTTPStatus

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient


def test_get_reports(test_app: FastAPI):
    with TestClient(test_app) as client:
        response = client.get("/reports")
        assert response.status_code == HTTPStatus.OK.value
        assert len(response.json()) == 2


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
