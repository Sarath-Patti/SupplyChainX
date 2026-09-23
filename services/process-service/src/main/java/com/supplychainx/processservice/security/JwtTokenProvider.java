package com.supplychainx.processservice.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.MalformedJwtException;
import io.jsonwebtoken.security.Keys;
import io.jsonwebtoken.security.SignatureException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.util.*;

@Component
public class JwtTokenProvider {

    private static final Logger log = LoggerFactory.getLogger(JwtTokenProvider.class);

    private final String secret;
    private final String issuer;
    private final String audience;
    private final SecretKey key;

    public JwtTokenProvider(
        @Value("${jwt.secret:SupplyChainX_Super_Secret_Jwt_Signing_Key_2026_Enterprise_Secure!}") String secret,
        @Value("${jwt.issuer:SupplyChainX}") String issuer,
        @Value("${jwt.audience:SupplyChainXClients}") String audience) {
        this.secret = secret;
        this.issuer = issuer;
        this.audience = audience;

        byte[] keyBytes = secret.getBytes(StandardCharsets.UTF_8);
        if (keyBytes.length < 32) {
            byte[] padded = new byte[32];
            System.arraycopy(keyBytes, 0, padded, 0, Math.min(keyBytes.length, 32));
            this.key = Keys.hmacShaKeyFor(padded);
        } else {
            this.key = Keys.hmacShaKeyFor(keyBytes);
        }
    }

    public boolean validateToken(String token) {
        try {
            Jwts.parser()
                .verifyWith(key)
                .build()
                .parseSignedClaims(token);
            return true;
        } catch (ExpiredJwtException e) {
            log.warn("Expired JWT token");
        } catch (MalformedJwtException e) {
            log.warn("Malformed JWT token");
        } catch (SignatureException e) {
            log.warn("Invalid JWT signature");
        } catch (IllegalArgumentException e) {
            log.warn("Empty or invalid JWT token string");
        } catch (Exception e) {
            log.warn("JWT token validation failed: {}", e.getMessage());
        }
        return false;
    }

    public Authentication getAuthentication(String token) {
        Claims claims = Jwts.parser()
            .verifyWith(key)
            .build()
            .parseSignedClaims(token)
            .getPayload();

        String username = claims.getSubject();
        if (username == null || username.isBlank()) {
            username = claims.get("name", String.class);
        }
        if (username == null || username.isBlank()) {
            username = "authenticatedUser";
        }

        List<GrantedAuthority> authorities = extractAuthorities(claims);

        return new UsernamePasswordAuthenticationToken(username, null, authorities);
    }

    private List<GrantedAuthority> extractAuthorities(Claims claims) {
        List<GrantedAuthority> authorities = new ArrayList<>();

        Object roleClaim = claims.get("role");
        if (roleClaim == null) {
            roleClaim = claims.get("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        }
        if (roleClaim == null) {
            roleClaim = claims.get("roles");
        }
        if (roleClaim == null) {
            roleClaim = claims.get("authorities");
        }

        if (roleClaim instanceof String rStr) {
            authorities.add(mapRoleToAuthority(rStr));
        } else if (roleClaim instanceof Collection<?> rList) {
            for (Object rObj : rList) {
                if (rObj != null) {
                    authorities.add(mapRoleToAuthority(rObj.toString()));
                }
            }
        }

        if (authorities.isEmpty()) {
            authorities.add(new SimpleGrantedAuthority("ROLE_USER"));
        }

        return authorities;
    }

    private SimpleGrantedAuthority mapRoleToAuthority(String roleStr) {
        String cleanRole = roleStr.toUpperCase().replace("ROLE_", "").trim();
        if ("ADMIN".equals(cleanRole)) {
            return new SimpleGrantedAuthority("ROLE_ADMIN");
        } else if ("ANALYST".equals(cleanRole) || "OPERATOR".equals(cleanRole)) {
            return new SimpleGrantedAuthority("ROLE_ANALYST");
        } else if ("USER".equals(cleanRole) || "VIEWER".equals(cleanRole)) {
            return new SimpleGrantedAuthority("ROLE_USER");
        }
        return new SimpleGrantedAuthority("ROLE_" + cleanRole);
    }

    public String generateTokenForTesting(String username, List<String> roles) {
        Date now = new Date();
        Date expiry = new Date(now.getTime() + 7200000); // 2 hours

        return Jwts.builder()
            .subject(username)
            .issuer(issuer)
            .audience().add(audience).and()
            .claim("role", roles)
            .issuedAt(now)
            .expiration(expiry)
            .signWith(key)
            .compact();
    }
}
