FROM python:3.9
RUN mkdir -p /app
WORKDIR /app
COPY . .

RUN pip install --upgrade pip
RUN pip install -e .[dev]
RUN pip install fastapi[all]
RUN pip install -i https://test.pypi.org/simple/ flotilla-openapi

EXPOSE 8000
RUN pip install gunicorn
CMD ["gunicorn", "--bind", "127.0.0.1:8000", "src/flotilla/main.py"]
