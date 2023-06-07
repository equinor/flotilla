import { Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage/FrontPage'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Typography } from '@equinor/eds-core-react'

const ButtonStyle = styled.div`
    display: flex;
`

const ButtonView = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`

export function AssetDecksButton({ refreshInterval }: RefreshProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/assetdecks`
        navigate(path)
    }

    return (
        <ButtonView>
            <Typography color="resting" variant="h2">
                {TranslateText('Asset Deck Inspections')}
            </Typography>
            <ButtonStyle>
                <Button variant="outlined" onClick={routeChange}>
                    {TranslateText('Asset Decks')}
                </Button>
            </ButtonStyle>
        </ButtonView>
    )
}
