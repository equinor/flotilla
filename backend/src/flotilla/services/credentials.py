from azure.core.exceptions import ClientAuthenticationError
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv


def get_azure_credentials():
    load_dotenv()
    try:
        return DefaultAzureCredential()
    except ClientAuthenticationError as e:
        raise e
