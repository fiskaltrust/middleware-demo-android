package eu.fiskaltrust.middleware.util;

import android.util.Base64;

import java.nio.charset.StandardCharsets;

public final class Base64UrlUtil {

  private static final int ENCODE_FLAGS = Base64.URL_SAFE | Base64.NO_WRAP | Base64.NO_PADDING;

  private Base64UrlUtil() { }

  public static String encode(String text) {
    return Base64.encodeToString(text.getBytes(StandardCharsets.UTF_8), ENCODE_FLAGS);
  }

  // Tolerant decoder: accepts both the base64url and the standard base64 alphabet,
  // with or without padding. The launcher does not guarantee a strictly url-safe
  // encoding for response content (e.g. the Restart & Pull Config response contains
  // '+'/'/'), and a strict URL_SAFE decode throws IllegalArgumentException.
  public static String decode(String base64Url) {
    String base64 = base64Url.replace('-', '+').replace('_', '/');
    return new String(Base64.decode(base64, Base64.DEFAULT), StandardCharsets.UTF_8);
  }
}
