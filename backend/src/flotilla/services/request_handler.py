from http import HTTPStatus
from typing import Any, Optional

import requests
from fastapi import HTTPException
from requests.exceptions import (
    ConnectionError,
    ConnectTimeout,
    HTTPError,
    RequestException,
    Timeout,
    TooManyRedirects,
)
from requests.models import Response

from flotilla.settings import settings


class RequestHandler:
    def __init__(self):
        pass

    def base_request(
        self,
        url: str,
        method: str,
        json_body: Any,
        timeout: float,
        auth: tuple,
        headers: Optional[dict] = None,
        data: Optional[dict] = None,
        params: Optional[dict] = None,
        **kwargs,
    ) -> Response:
        try:
            response = requests.request(
                url=url,
                method=method,
                auth=auth,
                headers=headers,
                timeout=timeout,
                json=json_body,
                data=data,
                params=params,
                **kwargs,
            )
            response.raise_for_status()
        except HTTPError as e:
            raise HTTPException(
                status_code=e.response.status_code,
                detail=e.response.reason,
            ) from e
        except Timeout as e:
            raise HTTPException(
                status_code=HTTPStatus.GATEWAY_TIMEOUT.value,
                detail=f"The request '{method} {url}'  timed out",
            ) from e
        except ConnectionError as e:
            raise HTTPException(
                status_code=HTTPStatus.BAD_GATEWAY.value,
                detail=f"A connection error occurred during the request '{method} {url}'",
            ) from e
        except TooManyRedirects as e:
            raise HTTPException(
                status_code=HTTPStatus.BAD_GATEWAY.value,
                detail=f"Too many redirects occurred during the request '{method} {url}'",
            ) from e
        except Exception as e:
            raise HTTPException(
                status_code=HTTPStatus.BAD_GATEWAY.value,
                detail=f"An ambiguous error occurred during the request '{method} {url}'",
            ) from e
        return response

    def get(
        self,
        url: str,
        json_body=None,
        request_timeout: float = settings.REQUEST_TIMEOUT,
        auth: Optional[tuple] = None,
        headers: Optional[dict] = None,
        data: Optional[dict] = None,
        params: Optional[dict] = None,
        **kwargs,
    ) -> Response:
        response = self.base_request(
            url=url,
            method="GET",
            auth=auth,
            headers=headers,
            timeout=request_timeout,
            json_body=json_body,
            data=data,
            params=params,
            **kwargs,
        )
        return response

    def post(
        self,
        url: str,
        json_body=None,
        request_timeout: float = settings.REQUEST_TIMEOUT,
        auth: Optional[tuple] = None,
        headers: Optional[dict] = None,
        data: Optional[dict] = None,
        params: Optional[dict] = None,
        **kwargs,
    ) -> Response:
        response = self.base_request(
            url=url,
            method="POST",
            auth=auth,
            headers=headers,
            timeout=request_timeout,
            json_body=json_body,
            data=data,
            params=params,
            **kwargs,
        )
        return response

    def delete(
        self,
        url: str,
        json_body=None,
        request_timeout: float = settings.REQUEST_TIMEOUT,
        auth: Optional[tuple] = None,
        headers: Optional[dict] = None,
        data: Optional[dict] = None,
        params: Optional[dict] = None,
        **kwargs,
    ) -> Response:
        response = self.base_request(
            url=url,
            method="DELETE",
            auth=auth,
            headers=headers,
            timeout=request_timeout,
            json_body=json_body,
            data=data,
            params=params,
            **kwargs,
        )
        return response

    def put(
        self,
        url: str,
        json_body=None,
        request_timeout: float = settings.REQUEST_TIMEOUT,
        auth: Optional[tuple] = None,
        headers: Optional[dict] = None,
        data: Optional[dict] = None,
        params: Optional[dict] = None,
        **kwargs,
    ) -> Response:
        response = self.base_request(
            url=url,
            method="PUT",
            auth=auth,
            headers=headers,
            timeout=request_timeout,
            json_body=json_body,
            data=data,
            params=params,
            **kwargs,
        )
        return response
