from setuptools import find_packages, setup

setup(
    name="flotilla",
    version="0.0.0",
    description="Backend for the Flotilla application",
    long_description=open("README.md").read(),
    long_description_content_type="text/markdown",
    author="Equinor ASA",
    author_email="fg_robots_dev@equinor.com",
    url="https://github.com/equinor/flotilla",
    packages=find_packages(where="src"),
    package_dir={"": "src"},
    classifiers=[
        "Environment :: Other Environment",
        "Intended Audience :: Developers",
        "Programming Language :: Python",
        "Topic :: Scientific/Engineering",
        "Topic :: Scientific/Engineering :: Physics",
    ],
    include_package_data=True,
    install_requires=[
        "azure-identity",
        "fastapi[all]",
        "fastapi-azure-auth",
        "pytz",
        "requests",
        "SQLAlchemy",
        "uvicorn",
    ],
    extras_require={"dev": ["pytest", "pytest-mock", "black", "mypy"]},
    python_requires=">=3.9",
)
