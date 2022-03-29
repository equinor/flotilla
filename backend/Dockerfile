FROM python:3.9

WORKDIR /app

ENV VIRTUAL_ENV=/venv
RUN python -m venv $VIRTUAL_ENV
ENV PATH="$VIRTUAL_ENV/bin:$PATH"

RUN python -m pip install --upgrade pip

# Install dependencies before Flotilla to cache pip installation
RUN mkdir -p src
COPY setup.py README.md ./
RUN pip install wheel
RUN pip install .

COPY . .

RUN pip install -e .[dev]
RUN pip install fastapi[all]
RUN pip install -i https://test.pypi.org/simple/ flotilla-openapi

EXPOSE 8000

# Change user to avoid running as root
# User needs to have an explicit guid for radix
RUN useradd -ms /bin/bash --uid 1001 isar
RUN chown -R 1001 /app
RUN chmod 755 /app
USER 1001


CMD ["uvicorn", "--host", "0.0.0.0", "src.flotilla.main:app"]
