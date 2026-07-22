import { Card, Icon, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { DocumentInfo } from 'models/DocumentInfo'
import { cardShadow } from 'components/Styles/StyledComponents'
import { phone_width } from 'utils/constants'

const StripCard = styled(Card)`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 1rem;
    padding: 1rem 1.5rem;
    box-shadow: ${cardShadow};
    box-sizing: border-box;
    @media (max-width: ${phone_width}) {
        padding: 1rem;
    }
`
const IconCircle = styled.div`
    display: flex;
    align-items: center;
    justify-content: center;
    width: 40px;
    height: 40px;
    border-radius: 50%;
    flex-shrink: 0;
    background: ${tokens.colors.ui.background__light.hex};
`
const TextBlock = styled.div`
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
`
const Subtitle = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    a {
        color: ${tokens.colors.interactive.primary__resting.hex};
        text-decoration: none;
    }
    a:hover {
        text-decoration: underline;
    }
`
const Spacer = styled.div`
    flex: 1 1 auto;
`
const Count = styled(Typography)`
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    white-space: nowrap;
    @media (max-width: ${phone_width}) {
        display: none;
    }
`

export const DocumentationSection = ({ documentation }: { documentation: DocumentInfo[] }) => {
    const { TranslateText } = useLanguageContext()
    const documentLabel = documentation.length === 1 ? TranslateText('document') : TranslateText('documents')

    return (
        <StripCard>
            <IconCircle>
                <Icon name={Icons.FileDescription} color={tokens.colors.interactive.primary__resting.hex} size={24} />
            </IconCircle>
            <TextBlock>
                <Typography variant="h5">{TranslateText('Documentation')}</Typography>
                <Subtitle>
                    {documentation.map((documentInfo, index) => (
                        <span key={index}>
                            <Typography
                                as="a"
                                variant="body_short"
                                href={documentInfo.url}
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                {documentInfo.name}
                            </Typography>
                            {index < documentation.length - 1 ? ',' : ''}
                        </span>
                    ))}
                </Subtitle>
            </TextBlock>
            <Spacer />
            <Count variant="caption">{`${documentation.length} ${documentLabel}`}</Count>
            <Icon name={Icons.RightCheveron} color={tokens.colors.text.static_icons__tertiary.hex} size={24} />
        </StripCard>
    )
}
