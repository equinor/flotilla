from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv


def get_azure_credentials():
    load_dotenv()
    return DefaultAzureCredential()
