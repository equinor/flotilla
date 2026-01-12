import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 10px;
`

export const PageLoading = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Header page={'loading'} />
            <Centered>
                <Typography variant="body_long_bold" color="primary">
                    {TranslateText('Loading page') + '...'}
                </Typography>
                <CircularProgress size={48} />
            </Centered>
        </>
    )
}
